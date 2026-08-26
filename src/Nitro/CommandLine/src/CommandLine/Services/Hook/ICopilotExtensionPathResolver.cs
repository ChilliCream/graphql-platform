namespace ChilliCream.Nitro.CommandLine.Services.Hook;

/// <summary>
/// Resolves the Copilot CLI project-scope extension paths this installer
/// writes: <c>&lt;repo-root&gt;/.github/extensions/nitro-mail/extension.mjs</c>
/// and its sibling <c>nitro-mail.config.json</c> (the launch descriptor the
/// asset reads at runtime, kept out of the versioned asset file itself so
/// the asset's bytes, and therefore its content hash, stay identical across
/// machines). There is no user-scope resolution path, unlike
/// <see cref="IClaudeSettingsPathResolver"/>'s user/project split.
/// </summary>
internal interface ICopilotExtensionPathResolver
{
    string ResolveExtensionFile();

    string ResolveConfigFile();
}
