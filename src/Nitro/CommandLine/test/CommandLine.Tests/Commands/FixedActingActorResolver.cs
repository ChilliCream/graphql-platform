using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands;

/// <summary>
/// Resolves an explicitly passed actor exactly as the real resolver does,
/// and falls back to a fixed actor instead of inspecting the harness
/// session, which no command test has.
/// </summary>
internal sealed class FixedActingActorResolver(string actor) : IActingActorResolver
{
    public Task<string> ResolveAsync(string? optionValue, CancellationToken cancellationToken)
        => Task.FromResult(
            optionValue is null ? actor : MailAgentName.Normalize(optionValue));
}
