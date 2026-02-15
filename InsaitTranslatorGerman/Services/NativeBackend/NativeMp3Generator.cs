using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace InsaitTranslatorGerman.Services.NativeBackend;

/// <summary>
/// Helper to generate an MP3 payload from the existing TextToSpeechService.
/// We write to a temp file because TextToSpeechService already exposes a file-based API.
/// </summary>
internal static class NativeMp3Generator
{
    public static async Task<byte[]> GenerateMp3Async(TextToSpeechService tts, string text, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(text))
            return Array.Empty<byte>();

        var tempFile = Path.Combine(Path.GetTempPath(), $"insait-tts-{Guid.NewGuid():N}.mp3");

        try
        {
            await tts.SaveToMp3Async(text, tempFile).ConfigureAwait(false);
            ct.ThrowIfCancellationRequested();
            return await File.ReadAllBytesAsync(tempFile, ct).ConfigureAwait(false);
        }
        finally
        {
            try
            {
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
            catch
            {
                // ignore cleanup failures
            }
        }
    }
}

