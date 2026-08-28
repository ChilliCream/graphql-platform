namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// A resolved workspace location. <see cref="ProjectDirectory"/> is the
/// project's identity root (the repository's main checkout for a git
/// workspace) and drives the default task prefix;
/// <see cref="CheckoutDirectory"/> is the checkout containing the start
/// directory, where sibling per-checkout files (such as
/// <c>.claude/settings.json</c>) belong. Outside git both are the directory
/// containing <c>.nitro</c>.
/// </summary>
internal readonly record struct WorkspaceLocation(
    string ProjectDirectory,
    string CheckoutDirectory,
    string WorkspaceDirectory);
