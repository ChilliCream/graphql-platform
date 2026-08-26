namespace ChilliCream.Nitro.CommandLine.Services.Hook;

internal enum CopilotExtensionInstallOutcome
{
    /// <summary>No file existed at the destination; it was created.</summary>
    Installed,

    /// <summary>
    /// A recognized prior asset version was on disk; it was replaced with
    /// the current version.
    /// </summary>
    Updated,

    /// <summary>The current asset version was already on disk; nothing was written.</summary>
    Unchanged,

    /// <summary>
    /// Content not matching any known asset version was on disk (a
    /// hand-edited or entirely foreign file) and <c>--force</c> was passed,
    /// so it was overwritten anyway.
    /// </summary>
    Forced
}
