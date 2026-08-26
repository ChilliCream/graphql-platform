namespace ChilliCream.Nitro.CommandLine.Services.Hook;

internal enum CopilotExtensionStatusOutcome
{
    Missing,
    Current,
    Outdated,

    /// <summary>
    /// On-disk content matches no asset version this CLI recognizes: not
    /// something <c>install</c> wrote, and not safe to overwrite without
    /// <c>--force</c>.
    /// </summary>
    Unrecognized
}
