namespace Mocha.Transport.Postgres;

internal static class ThrowHelper
{
    public static Exception BeforeAndAfterConflict()
        => Mocha.ThrowHelper.BeforeAndAfterConflict();

    public static Exception NotificationListenerStopped()
        => new InvalidOperationException("The notification listener cannot be started after it has stopped.");
}
