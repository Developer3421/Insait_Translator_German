using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace Insait_Translator_Deutsch.Services;

/// <summary>
/// Client used by the Browser (WASM) UI to call the native desktop backend.
/// </summary>
public sealed class NativeBackendClient : IDisposable
{
    private readonly HttpClient _http;

    public NativeBackendClient(string baseUrl = "http://127.0.0.1:5050")
    {
        _http = new HttpClient
        {
            BaseAddress = new Uri(baseUrl, UriKind.Absolute),
            Timeout = TimeSpan.FromMinutes(2)
        };
    }

    public async Task<string> TranslateAsync(string text, string sourceLang = "uk", string targetLang = "de")
    {
        var resp = await _http.PostAsJsonAsync("/api/translate", new { Text = text, SourceLang = sourceLang, TargetLang = targetLang })
            .ConfigureAwait(false);
        resp.EnsureSuccessStatusCode();

        var payload = await resp.Content.ReadFromJsonAsync<TranslateResponse>().ConfigureAwait(false);
        return payload?.translation ?? string.Empty;
    }

    public async Task<byte[]> SpeakMp3Async(string text)
    {
        var resp = await _http.PostAsJsonAsync("/api/speak-mp3", new { Text = text }).ConfigureAwait(false);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
    }

    public void Dispose() => _http.Dispose();

    private sealed record TranslateResponse(string translation);
}

