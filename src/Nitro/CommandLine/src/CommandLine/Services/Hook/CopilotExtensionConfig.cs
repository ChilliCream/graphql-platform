namespace ChilliCream.Nitro.CommandLine.Services.Hook;

/// <summary>
/// The <c>nitro-mail.config.json</c> content <c>extension.mjs</c> reads at
/// runtime to invoke <c>nitro</c>: the same executable-plus-argument-prefix
/// shape <see cref="LaunchDescriptor"/> carries, serialized so a Node.js
/// process (with no access to the .NET record) can read it.
/// </summary>
internal sealed record CopilotExtensionConfig(
    string Executable,
    IReadOnlyList<string> ArgumentPrefix,
    int ExtensionVersion,
    DateTimeOffset InstalledAt);
