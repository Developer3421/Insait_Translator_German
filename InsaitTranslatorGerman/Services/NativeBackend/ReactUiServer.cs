using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace InsaitTranslatorGerman.Services.NativeBackend;

/// <summary>
/// Simple HTTP server that serves the React UI static files (from ReactComponent/dist)
/// on port 4200. No Node.js/npm required at runtime.
/// </summary>
public sealed class ReactUiServer : IAsyncDisposable
{
    public const int DefaultPort = 4200;

    private readonly int _port;
    private readonly string? _distFolder;

    private HttpListener? _listener;
    private CancellationTokenSource? _cts;
    private Task? _loopTask;

    public ReactUiServer(int port = DefaultPort)
    {
        _port = port;
        _distFolder = FindDistFolder();
    }

    public bool IsRunning => _listener is { IsListening: true };
    public string Url => $"http://127.0.0.1:{_port}/";

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_listener != null)
            return Task.CompletedTask;

        if (_distFolder == null)
        {
            System.Diagnostics.Debug.WriteLine("[ReactUI] dist folder not found, UI server not started");
            return Task.CompletedTask;
        }

        var prefix = $"http://127.0.0.1:{_port}/";

        _listener = new HttpListener();
        _listener.Prefixes.Add(prefix);

        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _listener.Start();

        _loopTask = Task.Run(() => ListenLoopAsync(_cts.Token));
        System.Diagnostics.Debug.WriteLine($"[ReactUI] Server started at {prefix}");
        return Task.CompletedTask;
    }

    private string? FindDistFolder()
    {
        var possiblePaths = new[]
        {
            // From output directory (bin/Debug/net10.0)
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "ReactComponent", "dist"),
            // From solution root
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "ReactComponent", "dist"),
            // Direct path for development
            @"E:\Insait_Translator_Deutsch\InsaitTranslatorGerman\ReactComponent\dist",
        };

        foreach (var path in possiblePaths)
        {
            var fullPath = Path.GetFullPath(path);
            if (Directory.Exists(fullPath) && File.Exists(Path.Combine(fullPath, "index.html")))
            {
                System.Diagnostics.Debug.WriteLine($"[ReactUI] Found dist folder: {fullPath}");
                return fullPath;
            }
        }

        return null;
    }

    private async Task ListenLoopAsync(CancellationToken ct)
    {
        if (_listener == null) return;

        while (!ct.IsCancellationRequested && _listener.IsListening)
        {
            HttpListenerContext ctx;
            try
            {
                ctx = await _listener.GetContextAsync().ConfigureAwait(false);
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch (HttpListenerException)
            {
                break;
            }
            catch
            {
                continue;
            }

            _ = Task.Run(() => HandleRequestAsync(ctx, ct), ct);
        }
    }

    private async Task HandleRequestAsync(HttpListenerContext ctx, CancellationToken ct)
    {
        try
        {
            var req = ctx.Request;
            var res = ctx.Response;

            // CORS headers
            res.Headers["Access-Control-Allow-Origin"] = "*";
            res.Headers["Access-Control-Allow-Headers"] = "Content-Type";
            res.Headers["Access-Control-Allow-Methods"] = "GET,OPTIONS";

            if (req.HttpMethod.Equals("OPTIONS", StringComparison.OrdinalIgnoreCase))
            {
                res.StatusCode = 204;
                res.Close();
                return;
            }

            var path = req.Url?.AbsolutePath ?? "/";
            
            // Normalize path
            if (path == "/") path = "/index.html";
            
            // Security: prevent directory traversal
            path = path.Replace("..", "").TrimStart('/');

            var filePath = Path.Combine(_distFolder!, path);

            // If file doesn't exist, serve index.html (SPA fallback)
            if (!File.Exists(filePath))
            {
                filePath = Path.Combine(_distFolder!, "index.html");
            }

            if (File.Exists(filePath))
            {
                var content = await File.ReadAllBytesAsync(filePath, ct).ConfigureAwait(false);
                var contentType = GetContentType(filePath);

                res.StatusCode = 200;
                res.ContentType = contentType;
                res.ContentLength64 = content.LongLength;
                
                // Cache static assets
                if (!filePath.EndsWith("index.html"))
                {
                    res.Headers["Cache-Control"] = "public, max-age=31536000";
                }

                await res.OutputStream.WriteAsync(content, 0, content.Length, ct).ConfigureAwait(false);
            }
            else
            {
                res.StatusCode = 404;
                var notFound = Encoding.UTF8.GetBytes("Not Found");
                res.ContentType = "text/plain";
                res.ContentLength64 = notFound.Length;
                await res.OutputStream.WriteAsync(notFound, 0, notFound.Length, ct).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ReactUI] Error handling request: {ex.Message}");
        }
        finally
        {
            try { ctx.Response.Close(); } catch { }
        }
    }

    private static string GetContentType(string filePath)
    {
        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        return ext switch
        {
            ".html" => "text/html; charset=utf-8",
            ".htm" => "text/html; charset=utf-8",
            ".css" => "text/css; charset=utf-8",
            ".js" => "application/javascript; charset=utf-8",
            ".mjs" => "application/javascript; charset=utf-8",
            ".json" => "application/json; charset=utf-8",
            ".png" => "image/png",
            ".jpg" => "image/jpeg",
            ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".svg" => "image/svg+xml",
            ".ico" => "image/x-icon",
            ".woff" => "font/woff",
            ".woff2" => "font/woff2",
            ".ttf" => "font/ttf",
            ".eot" => "application/vnd.ms-fontobject",
            ".mp3" => "audio/mpeg",
            ".wav" => "audio/wav",
            ".webp" => "image/webp",
            _ => "application/octet-stream"
        };
    }

    public async ValueTask DisposeAsync()
    {
        var listener = _listener;
        if (listener == null) return;

        try
        {
            _cts?.Cancel();
            listener.Stop();
            listener.Close();

            if (_loopTask != null)
                await _loopTask.ConfigureAwait(false);

            System.Diagnostics.Debug.WriteLine("[ReactUI] Server stopped");
        }
        finally
        {
            _cts?.Dispose();
            _cts = null;
            _listener = null;
            _loopTask = null;
        }
    }
}

