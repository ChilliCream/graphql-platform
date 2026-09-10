namespace ChilliCream.Nitro.CommandLine.Services.Tasks;

using ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// Resolves the acting identity recorded on task mutations.
/// </summary>
internal static class TaskActor
{
    public static Task<string> ResolveAsync(
        string? optionValue,
        IActingActorResolver resolver,
        CancellationToken cancellationToken)
        => resolver.ResolveAsync(optionValue, cancellationToken);
}
