namespace ChilliCream.Nitro.CommandLine.Services.Memory;

using ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// Resolves the acting identity recorded on memory writes.
/// </summary>
internal static class MemoryActor
{
    public static Task<string> ResolveAsync(
        string? optionValue,
        IActingActorResolver resolver,
        CancellationToken cancellationToken)
        => resolver.ResolveAsync(optionValue, cancellationToken);
}
