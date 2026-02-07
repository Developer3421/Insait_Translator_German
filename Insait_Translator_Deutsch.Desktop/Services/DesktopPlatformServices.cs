using System;
using System.Diagnostics;
using Insait_Translator_Deutsch.Services;

namespace Insait_Translator_Deutsch.Desktop.Services;

public sealed class DesktopPlatformServices : IPlatformServices
{
    public IOpenUrlService OpenUrl { get; } = new DesktopOpenUrlService();
    public INotificationsService Notifications { get; } = new DesktopNotificationsService();

    private sealed class DesktopOpenUrlService : IOpenUrlService
    {
        public void Open(Uri uri)
        {
            try
            {
                // UseShellExecute=true is important on Windows for opening URLs.
                Process.Start(new ProcessStartInfo
                {
                    FileName = uri.ToString(),
                    UseShellExecute = true
                });
            }
            catch
            {
                // ignore
            }
        }
    }

    private sealed class DesktopNotificationsService : INotificationsService
    {
        public void Notify(string title, string message)
        {
            // Minimal implementation: no native toast dependency.
            // The UI already has StatusText; use that instead when needed.
            Debug.WriteLine($"{title}: {message}");
        }
    }
}

