namespace Insait_Translator_Deutsch.Services;

/// <summary>
/// Minimal app-level service locator so the core Avalonia app can use platform-specific features
/// (Desktop/Android/iOS) without pulling platform runtime dependencies into the shared project.
/// </summary>
public static class AppServices
{
    /// <summary>
    /// Set by platform head projects (Desktop/Android/iOS). Defaults to a no-op implementation.
    /// </summary>
    public static IPlatformServices Current { get; set; } = new DefaultPlatformServices();
}
