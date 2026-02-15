using System;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace InsaitTranslatorGerman.Services.NativeBackend;

/// <summary>
/// Lightweight local HTTP backend that allows the embedded/companion React UI to call into
/// the native desktop app (translation + TTS). Bound to loopback only.
/// </summary>
public sealed class NativeBackendServer : IAsyncDisposable
{
    public const int DefaultPort = 4201;

    private readonly TranslationService _translation;
    private readonly TextToSpeechService _tts;
    private readonly int _port;

    private HttpListener? _listener;
    private CancellationTokenSource? _cts;
    private Task? _loopTask;

    public NativeBackendServer(TranslationService translation, TextToSpeechService tts, int port = DefaultPort)
    {
        _translation = translation;
        _tts = tts;
        _port = port;
    }

    public bool IsRunning => _listener is { IsListening: true };

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_listener != null)
            return Task.CompletedTask;

        // HttpListener wants a trailing slash.
        var prefix = $"http://127.0.0.1:{_port}/";

        _listener = new HttpListener();
        _listener.Prefixes.Add(prefix);

        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _listener.Start();

        _loopTask = Task.Run(() => ListenLoopAsync(_cts.Token));
        return Task.CompletedTask;
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
                // listener stopped
                break;
            }
            catch
            {
                continue;
            }

            _ = Task.Run(() => HandleAsync(ctx, ct), ct);
        }
    }

    private async Task HandleAsync(HttpListenerContext ctx, CancellationToken ct)
    {
        try
        {
            var req = ctx.Request;
            var res = ctx.Response;

            // Basic CORS for local React dev server (optional, harmless if unused).
            res.Headers["Access-Control-Allow-Origin"] = "*";
            res.Headers["Access-Control-Allow-Headers"] = "Content-Type";
            res.Headers["Access-Control-Allow-Methods"] = "GET,POST,OPTIONS";

            if (req.HttpMethod.Equals("OPTIONS", StringComparison.OrdinalIgnoreCase))
            {
                res.StatusCode = 204;
                res.Close();
                return;
            }

            var path = req.Url?.AbsolutePath ?? "/";

            if (req.HttpMethod.Equals("GET", StringComparison.OrdinalIgnoreCase) && (path == "/health" || path == "/api/health"))
            {
                // React expects JSON: { ok: true }
                var json = JsonSerializer.Serialize(new { ok = true });
                await WriteBytesAsync(res, Encoding.UTF8.GetBytes(json), "application/json; charset=utf-8", 200, ct)
                    .ConfigureAwait(false);
                return;
            }

            if (req.HttpMethod.Equals("POST", StringComparison.OrdinalIgnoreCase) && path == "/api/translate")
            {
                var body = await ReadBodyAsync(req, ct).ConfigureAwait(false);
                var payload = JsonSerializer.Deserialize<TranslateRequest>(body,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                var text = payload?.Text ?? string.Empty;
                var source = string.IsNullOrWhiteSpace(payload?.SourceLang) ? "uk" : payload.SourceLang;
                var target = string.IsNullOrWhiteSpace(payload?.TargetLang) ? "de" : payload.TargetLang;

                var translation = await _translation.TranslateAsync(text, source!, target!).ConfigureAwait(false);

                var json = JsonSerializer.Serialize(new TranslateResponse(translation));
                await WriteBytesAsync(res, Encoding.UTF8.GetBytes(json), "application/json; charset=utf-8", 200, ct)
                    .ConfigureAwait(false);
                return;
            }

            if (req.HttpMethod.Equals("POST", StringComparison.OrdinalIgnoreCase) && path == "/api/speak-mp3")
            {
                var body = await ReadBodyAsync(req, ct).ConfigureAwait(false);
                var payload = JsonSerializer.Deserialize<SpeakRequest>(body,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                var text = payload?.Text ?? string.Empty;

                if (!_tts.IsInitialized)
                    await _tts.InitializeAsync().ConfigureAwait(false);

                var mp3 = await InsaitTranslatorGerman.Services.NativeBackend.NativeMp3Generator
                    .GenerateMp3Async(_tts, text, ct)
                    .ConfigureAwait(false);

                await WriteBytesAsync(res, mp3, "audio/mpeg", 200, ct).ConfigureAwait(false);
                return;
            }

            await WriteTextAsync(res, "Not found", "text/plain; charset=utf-8", ct, statusCode: 404)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            try
            {
                await WriteTextAsync(ctx.Response, ex.Message, "text/plain; charset=utf-8", ct, statusCode: 500)
                    .ConfigureAwait(false);
            }
            catch
            {
                // ignore
            }
        }
        finally
        {
            try { ctx.Response.Close(); } catch { /* ignore */ }
        }
    }

    private static async Task<string> ReadBodyAsync(HttpListenerRequest req, CancellationToken ct)
    {
        // HttpListenerRequest.InputStream doesn't support cancellation well; we at least respect it between awaits.
        using var reader = new StreamReader(req.InputStream, req.ContentEncoding ?? Encoding.UTF8);
        var body = await reader.ReadToEndAsync().ConfigureAwait(false);
        ct.ThrowIfCancellationRequested();
        return body;
    }

    private static Task WriteTextAsync(HttpListenerResponse res, string text, string contentType, CancellationToken ct,
        int statusCode = 200)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        return WriteBytesAsync(res, bytes, contentType, statusCode, ct);
    }

    private static async Task WriteBytesAsync(HttpListenerResponse res, byte[] bytes, string contentType, int statusCode,
        CancellationToken ct)
    {
        res.StatusCode = statusCode;
        res.ContentType = contentType;
        res.ContentLength64 = bytes.LongLength;
        await res.OutputStream.WriteAsync(bytes, 0, bytes.Length, ct).ConfigureAwait(false);
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
        }
        finally
        {
            _cts?.Dispose();
            _cts = null;
            _listener = null;
            _loopTask = null;
        }
    }

    private sealed record TranslateRequest(string Text, string SourceLang, string TargetLang);
    private sealed record SpeakRequest(string Text);
    private sealed record TranslateResponse(string translation);
}

