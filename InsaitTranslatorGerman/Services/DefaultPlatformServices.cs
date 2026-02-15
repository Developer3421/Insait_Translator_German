using System;

namespace InsaitTranslatorGerman.Services;

public sealed class DefaultPlatformServices : IPlatformServices
{
    public IOpenUrlService OpenUrl { get; } = new NoOpOpenUrlService();
    public INotificationsService Notifications { get; } = new NoOpNotificationsService();

    private sealed class NoOpOpenUrlService : IOpenUrlService
    {
        public void Open(Uri uri)
        {
            // no-op
        }
    }

    private sealed class NoOpNotificationsService : INotificationsService
    {
        public void Notify(string title, string message)
        {
            // no-op
        }
    }
}
