using System;

namespace Insait_Translator_Deutsch.Services;

public interface IPlatformServices
{
    IOpenUrlService OpenUrl { get; }
    INotificationsService Notifications { get; }
}

public interface IOpenUrlService
{
    void Open(Uri uri);
}

public interface INotificationsService
{
    /// <summary>Fire-and-forget user-visible notification (native if possible).</summary>
    void Notify(string title, string message);
}

