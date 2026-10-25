namespace ChilliCream.Nitro.CommandLine.Services.Hook;

/// <summary>
/// The state of one managed hook entry. <see cref="Disabled"/> is a current entry the harness
/// skips because its own <c>enabled</c> flag is <c>false</c>.
/// </summary>
internal enum HookStatusOutcome { Missing, Installed, Outdated, Disabled }
