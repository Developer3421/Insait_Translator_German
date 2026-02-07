using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Insait_Translator_Deutsch.Services;

namespace Insait_Translator_Deutsch.Desktop.NativeBackend;

/// <summary>
/// Minimal local HTTP backend that exposes translation + TTS/MP3 generation to the Browser UI.
/// This keeps the WebAssembly app as UI-only.
/// </summary>
public sealed class NativeBackendHost : IAsyncDisposable
{
    private readonly HttpListener _listener = new();
    private readonly TranslationService _translation = new();
    private readonly TextToSpeechService _tts = new();
    private readonly Uri? _uiProxyBaseAddress;
    private readonly HttpClient? _proxyClient;
    private CancellationTokenSource? _cts;
    private Task? _loop;
    private bool _ttsInitialized;

    public Uri BaseAddress { get; private set; }

    public NativeBackendHost(string urlPrefix = "http://127.0.0.1:5050/", string? uiProxyBaseUrl = null)
    {
        BaseAddress = new Uri(urlPrefix);
        _listener.Prefixes.Add(urlPrefix);

        if (!string.IsNullOrWhiteSpace(uiProxyBaseUrl))
        {
            _uiProxyBaseAddress = new Uri(uiProxyBaseUrl, UriKind.Absolute);
            _proxyClient = new HttpClient(new HttpClientHandler
            {
                AllowAutoRedirect = false,
                AutomaticDecompression = DecompressionMethods.None
            })
            {
                Timeout = TimeSpan.FromSeconds(30)
            };
        }
    }

    public void Start()
    {
        _cts = new CancellationTokenSource();

        string startedPrefix;
        try
        {
            _listener.Start();
            startedPrefix = BaseAddress.ToString();
        }
        catch (HttpListenerException ex)
        {
            // Якщо 127.0.0.1 не працює, спробуємо localhost
            System.Diagnostics.Debug.WriteLine($"[NativeBackendHost] Failed to start on {BaseAddress}: {ex.Message}");
            Console.WriteLine($"[NativeBackendHost] Failed to start on {BaseAddress}: {ex.Message}");

            // Спробуємо альтернативний URL
            var altUrl = BaseAddress.ToString().Replace("127.0.0.1", "localhost");
            System.Diagnostics.Debug.WriteLine($"[NativeBackendHost] Trying alternative: {altUrl}");

            _listener.Prefixes.Clear();
            _listener.Prefixes.Add(altUrl);
            _listener.Start();

            BaseAddress = new Uri(altUrl);
            startedPrefix = altUrl;
        }

        _loop = Task.Run(() => AcceptLoopAsync(_cts.Token));

        System.Diagnostics.Debug.WriteLine($"[NativeBackendHost] Started on {startedPrefix}");
        Console.WriteLine($"[NativeBackendHost] Started on {startedPrefix}");

        // Log embedded resources for debugging
        var assembly = Assembly.GetExecutingAssembly();
        var resources = assembly.GetManifestResourceNames();
        System.Diagnostics.Debug.WriteLine($"[NativeBackendHost] Found {resources.Length} embedded resources:");
        foreach (var res in resources)
        {
            System.Diagnostics.Debug.WriteLine($"  - {res}");
        }
    }

    private async Task AcceptLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            HttpListenerContext ctx;
            try
            {
                ctx = await _listener.GetContextAsync().ConfigureAwait(false);
            }
            catch when (ct.IsCancellationRequested)
            {
                break;
            }
            catch
            {
                continue;
            }

            _ = Task.Run(() => HandleAsync(ctx), ct);
        }
    }

    private async Task HandleAsync(HttpListenerContext ctx)
    {
        try
        {
            // CORS for wasm
            ctx.Response.AddHeader("Access-Control-Allow-Origin", "*");
            ctx.Response.AddHeader("Access-Control-Allow-Methods", "GET,POST,OPTIONS");
            ctx.Response.AddHeader("Access-Control-Allow-Headers", "content-type");

            if (ctx.Request.HttpMethod == "OPTIONS")
            {
                ctx.Response.StatusCode = 204;
                ctx.Response.Close();
                return;
            }

            var path = ctx.Request.Url?.AbsolutePath ?? "/";

            if (ctx.Request.HttpMethod == "GET" && path == "/api/health")
            {
                await WriteJsonAsync(ctx, new { ok = true }).ConfigureAwait(false);
                return;
            }

            if (ctx.Request.HttpMethod == "POST" && path == "/api/translate")
            {
                try
                {
                    var body = await JsonSerializer.DeserializeAsync<TranslateRequest>(ctx.Request.InputStream, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }).ConfigureAwait(false);

                    var text = body?.Text ?? string.Empty;
                    var source = body?.SourceLang ?? "uk";
                    var target = body?.TargetLang ?? "de";

                    System.Diagnostics.Debug.WriteLine($"[NativeBackendHost] Translating: '{text.Substring(0, Math.Min(50, text.Length))}...' from {source} to {target}");
                    
                    var translation = await _translation.TranslateAsync(text, source, target).ConfigureAwait(false);
                    
                    System.Diagnostics.Debug.WriteLine($"[NativeBackendHost] Translation successful: '{translation.Substring(0, Math.Min(50, translation.Length))}...'");
                    
                    await WriteJsonAsync(ctx, new { translation }).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[NativeBackendHost] Translation error: {ex}");
                    Console.WriteLine($"[NativeBackendHost] Translation error: {ex}");
                    ctx.Response.StatusCode = 500;
                    await WriteJsonAsync(ctx, new { error = ex.Message, details = ex.ToString() }).ConfigureAwait(false);
                }
                return;
            }

            if (ctx.Request.HttpMethod == "POST" && path == "/api/speak-mp3")
            {
                try
                {
                    var body = await JsonSerializer.DeserializeAsync<SpeakRequest>(ctx.Request.InputStream, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }).ConfigureAwait(false);

                    var text = body?.Text ?? string.Empty;

                    System.Diagnostics.Debug.WriteLine($"[NativeBackendHost] TTS request for text: '{text.Substring(0, Math.Min(50, text.Length))}...'");

                    // Initialize TTS on first use
                    if (!_ttsInitialized)
                    {
                        System.Diagnostics.Debug.WriteLine("[NativeBackendHost] Initializing TTS...");
                        await _tts.InitializeAsync(status => System.Diagnostics.Debug.WriteLine($"[NativeBackendHost] TTS: {status}")).ConfigureAwait(false);
                        _ttsInitialized = true;
                        System.Diagnostics.Debug.WriteLine("[NativeBackendHost] TTS initialized successfully");
                    }

                    // Create temp mp3 and return bytes - use AppDataPaths for Microsoft Store compatibility
                    var tempMp3 = AppDataPaths.GetTempFilePath(".mp3");
                    try
                    {
                        await _tts.SaveToMp3Async(text, tempMp3).ConfigureAwait(false);
                        var bytes = await System.IO.File.ReadAllBytesAsync(tempMp3).ConfigureAwait(false);

                        System.Diagnostics.Debug.WriteLine($"[NativeBackendHost] TTS generated {bytes.Length} bytes");

                        ctx.Response.ContentType = "audio/mpeg";
                        ctx.Response.ContentLength64 = bytes.Length;
                        await ctx.Response.OutputStream.WriteAsync(bytes, 0, bytes.Length).ConfigureAwait(false);
                        ctx.Response.Close();
                    }
                    finally
                    {
                        try { if (System.IO.File.Exists(tempMp3)) System.IO.File.Delete(tempMp3); } catch { }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[NativeBackendHost] TTS error: {ex}");
                    Console.WriteLine($"[NativeBackendHost] TTS error: {ex}");
                    ctx.Response.StatusCode = 500;
                    await WriteJsonAsync(ctx, new { error = ex.Message, details = ex.ToString() }).ConfigureAwait(false);
                }

                return;
            }

            // UI: if dev proxy configured, forward everything except /api/* to React dev server
            if (_uiProxyBaseAddress != null && _proxyClient != null)
            {
                var isApi = path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase) || path.Equals("/api", StringComparison.OrdinalIgnoreCase);
                if (!isApi)
                {
                    var proxied = await TryProxyToUiDevServerAsync(ctx, _uiProxyBaseAddress, _proxyClient).ConfigureAwait(false);
                    if (proxied) return;
                }
            }

            // Serve embedded React static files
            if (ctx.Request.HttpMethod == "GET")
            {
                var served = await TryServeEmbeddedFileAsync(ctx, path).ConfigureAwait(false);
                if (served) return;
            }

            ctx.Response.StatusCode = 404;
            await WriteTextAsync(ctx, "Not found").ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            try
            {
                ctx.Response.StatusCode = 500;
                await WriteJsonAsync(ctx, new { error = ex.Message }).ConfigureAwait(false);
            }
            catch
            {
                // ignore
            }
        }
    }

    private static async Task WriteJsonAsync(HttpListenerContext ctx, object obj)
    {
        var json = JsonSerializer.Serialize(obj);
        var bytes = Encoding.UTF8.GetBytes(json);
        ctx.Response.ContentType = "application/json; charset=utf-8";
        ctx.Response.ContentLength64 = bytes.Length;
        await ctx.Response.OutputStream.WriteAsync(bytes, 0, bytes.Length).ConfigureAwait(false);
        ctx.Response.Close();
    }

    private static async Task WriteTextAsync(HttpListenerContext ctx, string text)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        ctx.Response.ContentType = "text/plain; charset=utf-8";
        ctx.Response.ContentLength64 = bytes.Length;
        await ctx.Response.OutputStream.WriteAsync(bytes, 0, bytes.Length).ConfigureAwait(false);
        ctx.Response.Close();
    }

    private static async Task<bool> TryProxyToUiDevServerAsync(HttpListenerContext ctx, Uri uiBase, HttpClient client)
    {
        // HttpListener doesn't handle websocket upgrades well; for desktop dev we proxy plain HTTP only.
        // If the dev server is down, return false so caller can fall back to embedded resources.

        var req = ctx.Request;
        var targetUri = new Uri(uiBase, req.RawUrl ?? "/");

        using var msg = new HttpRequestMessage(new HttpMethod(req.HttpMethod), targetUri);

        // Copy request headers (skip hop-by-hop)
        foreach (var headerName in req.Headers.AllKeys)
        {
            if (string.IsNullOrWhiteSpace(headerName)) continue;
            if (IsHopByHopHeader(headerName)) continue;
            if (headerName.Equals("Host", StringComparison.OrdinalIgnoreCase)) continue;

            var value = req.Headers[headerName];
            if (string.IsNullOrEmpty(value)) continue;

            // Some headers must go to Content headers, but we only attach content if needed.
            msg.Headers.TryAddWithoutValidation(headerName, value);
        }

        // Body
        if (req.HasEntityBody)
        {
            msg.Content = new StreamContent(req.InputStream);
            if (!string.IsNullOrWhiteSpace(req.ContentType))
                msg.Content.Headers.TryAddWithoutValidation("Content-Type", req.ContentType);
        }

        HttpResponseMessage? resp;
        try
        {
            resp = await client.SendAsync(msg, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
        }
        catch
        {
            return false;
        }

        using (resp)
        {
            ctx.Response.StatusCode = (int)resp.StatusCode;

            foreach (var header in resp.Headers)
            {
                if (IsHopByHopHeader(header.Key)) continue;
                foreach (var v in header.Value)
                    ctx.Response.AddHeader(header.Key, v);
            }

            foreach (var header in resp.Content.Headers)
            {
                if (IsHopByHopHeader(header.Key)) continue;
                foreach (var v in header.Value)
                    ctx.Response.AddHeader(header.Key, v);
            }

            await using var responseStream = await resp.Content.ReadAsStreamAsync().ConfigureAwait(false);
            await responseStream.CopyToAsync(ctx.Response.OutputStream).ConfigureAwait(false);

            ctx.Response.Close();
            return true;
        }
    }

    private static bool IsHopByHopHeader(string headerName)
    {
        // RFC 2616 13.5.1
        return headerName.Equals("Connection", StringComparison.OrdinalIgnoreCase)
               || headerName.Equals("Keep-Alive", StringComparison.OrdinalIgnoreCase)
               || headerName.Equals("Proxy-Authenticate", StringComparison.OrdinalIgnoreCase)
               || headerName.Equals("Proxy-Authorization", StringComparison.OrdinalIgnoreCase)
               || headerName.Equals("TE", StringComparison.OrdinalIgnoreCase)
               || headerName.Equals("Trailers", StringComparison.OrdinalIgnoreCase)
               || headerName.Equals("Transfer-Encoding", StringComparison.OrdinalIgnoreCase)
               || headerName.Equals("Upgrade", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<bool> TryServeEmbeddedFileAsync(HttpListenerContext ctx, string path)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // Normalize path: remove leading slash, default to index.html
        var filePath = path.TrimStart('/');
        if (string.IsNullOrEmpty(filePath))
            filePath = "index.html";

        // Preferred match: by relative path under wwwroot (as linked in .csproj)
        var resourcePath = filePath.Replace('/', '.').Replace('\\', '.');
        var expectedSuffixes = new[]
        {
            $".wwwroot.{resourcePath}",
            $".wwwroot.{filePath.Replace('/', '.')}",
            $".wwwroot.{Path.GetFileName(filePath)}"
        };

        var resourceNames = assembly.GetManifestResourceNames();
        string? matchedResource = null;

        foreach (var name in resourceNames)
        {
            foreach (var suf in expectedSuffixes)
            {
                if (name.EndsWith(suf, StringComparison.OrdinalIgnoreCase))
                {
                    matchedResource = name;
                    break;
                }
            }
            if (matchedResource != null) break;
        }

        // Fallback: try hashed React files (index-HASH.js, index-HASH.css)
        if (matchedResource == null)
        {
            var fileName = Path.GetFileName(filePath);
            var fileNameWithoutExt = Path.GetFileNameWithoutExtension(filePath);
            var ext = Path.GetExtension(filePath);

            foreach (var name in resourceNames)
            {
                if (name.EndsWith($".{fileName}", StringComparison.OrdinalIgnoreCase) ||
                    name.EndsWith($".wwwroot.{fileName}", StringComparison.OrdinalIgnoreCase))
                {
                    matchedResource = name;
                    break;
                }

                if (!string.IsNullOrEmpty(ext))
                {
                    var baseName = fileNameWithoutExt.Split('-')[0];
                    if (name.Contains($".{baseName}-", StringComparison.OrdinalIgnoreCase) &&
                        name.EndsWith(ext, StringComparison.OrdinalIgnoreCase))
                    {
                        matchedResource = name;
                        break;
                    }
                }
            }
        }

        // SPA fallback
        if (matchedResource == null && !Path.HasExtension(filePath))
        {
            foreach (var name in resourceNames)
            {
                if (name.EndsWith(".wwwroot.index.html", StringComparison.OrdinalIgnoreCase) ||
                    name.EndsWith(".index.html", StringComparison.OrdinalIgnoreCase))
                {
                    matchedResource = name;
                    break;
                }
            }
        }

        if (matchedResource == null)
            return false;

        await using var stream = assembly.GetManifestResourceStream(matchedResource);
        if (stream == null)
            return false;

        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms).ConfigureAwait(false);
        var bytes = ms.ToArray();

        ctx.Response.ContentType = GetMimeType(matchedResource);
        ctx.Response.ContentLength64 = bytes.Length;
        await ctx.Response.OutputStream.WriteAsync(bytes, 0, bytes.Length).ConfigureAwait(false);
        ctx.Response.Close();
        return true;
    }

    private static string GetMimeType(string path)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        return ext switch
        {
            ".html" => "text/html; charset=utf-8",
            ".css" => "text/css; charset=utf-8",
            ".js" => "application/javascript; charset=utf-8",
            ".json" => "application/json; charset=utf-8",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".svg" => "image/svg+xml",
            ".ico" => "image/x-icon",
            ".woff" => "font/woff",
            ".woff2" => "font/woff2",
            ".ttf" => "font/ttf",
            ".eot" => "application/vnd.ms-fontobject",
            ".txt" => "text/plain; charset=utf-8",
            _ => "application/octet-stream"
        };
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            _cts?.Cancel();
        }
        catch { }

        try
        {
            if (_listener.IsListening)
                _listener.Stop();
        }
        catch { }

        try
        {
            if (_loop != null)
                await _loop.ConfigureAwait(false);
        }
        catch { }

        _listener.Close();
        _translation.Dispose();
        _tts.Dispose();
    }

    private sealed record TranslateRequest(string Text, string SourceLang, string TargetLang);
    private sealed record SpeakRequest(string Text);
}

