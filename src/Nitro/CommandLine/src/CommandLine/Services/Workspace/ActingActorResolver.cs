using ChilliCream.Nitro.CommandLine.Services.Mail;

namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

internal sealed class ActingActorResolver(IAgentStore agents) : IActingActorResolver
{
    public async Task<string> ResolveAsync(
        string? optionValue,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(optionValue))
        {
            throw new ExitException(
                "No actor given. Pass '--actor <actor>', and run `nitro agent login` to "
                + "allocate one if this session has none.");
        }

        var actor = MailAgentName.Normalize(optionValue);

        var existing = await agents.FindAsync(actor, cancellationToken);

        if (existing is null)
        {
            throw new ExitException(
                $"Unknown actor '{actor}'. Run `nitro agent login` to allocate one, "
                + "or `nitro agent list` to see the actors this workspace knows.");
        }

        if (existing.IsDeleted)
        {
            throw new ExitException($"Agent '{existing.Name}' was deleted.");
        }

        // Every command run with --actor is a presence beat.
        await agents.TouchAsync(actor, cancellationToken);

        return actor;
    }
}
