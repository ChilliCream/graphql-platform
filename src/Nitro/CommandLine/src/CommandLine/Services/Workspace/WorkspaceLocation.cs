namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// The project identity root, current checkout directory, and shared workspace
/// directory. In the fallback layout, the project and checkout directories both
/// contain <c>.nitro</c>.
/// </summary>
internal readonly record struct WorkspaceLocation(
    string ProjectDirectory,
    string CheckoutDirectory,
    string WorkspaceDirectory);
