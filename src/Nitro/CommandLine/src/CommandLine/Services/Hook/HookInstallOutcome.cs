namespace ChilliCream.Nitro.CommandLine.Services.Hook;

/// <summary>
/// What an install did to one managed hook entry. <see cref="Disabled"/> is an entry left
/// current but switched off, because its own <c>enabled</c> flag is <c>false</c>.
/// </summary>
internal enum HookInstallOutcome { Installed, Updated, Unchanged, Disabled }
