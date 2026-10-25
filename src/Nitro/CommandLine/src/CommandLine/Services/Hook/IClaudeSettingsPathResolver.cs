namespace ChilliCream.Nitro.CommandLine.Services.Hook;

/// <summary>
/// Resolves the settings file a hook install scope edits for a harness speaking Claude Code's
/// hook format.
/// </summary>
internal interface IClaudeSettingsPathResolver
{
    string Resolve(string scope);

    /// <summary>
    /// The settings file of each install scope whose hooks the harness runs for a session started
    /// in the current directory, the user scope's file first.
    /// </summary>
    IReadOnlyList<string> ResolveForCurrentDirectory();
}
