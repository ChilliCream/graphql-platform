using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands;

/// <summary>
/// Normalizes an explicit actor name without checking allocation.
/// Returns <paramref name="actor"/> unchanged when the option value is null.
/// </summary>
internal sealed class FixedActingActorResolver(string actor) : IActingActorResolver
{
    public Task<string> ResolveAsync(string? optionValue, CancellationToken cancellationToken)
        => Task.FromResult(
            optionValue is null ? actor : MailAgentName.Normalize(optionValue));
}
