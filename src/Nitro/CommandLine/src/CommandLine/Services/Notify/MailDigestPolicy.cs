namespace ChilliCream.Nitro.CommandLine.Services.Notify;

internal static class MailDigestPolicy
{
    public const int MaxMessages = 10;
    public const int MaxBodyChars = 4_000;
    public const int MaxTotalBytes = 32_768;
}
