namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

internal interface IActingActorResolver
{
    Task<string> ResolveAsync(
        string? optionValue,
        CancellationToken cancellationToken);
}
