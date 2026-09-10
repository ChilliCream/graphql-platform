using ChilliCream.Nitro.CommandLine.Services.Mail;

namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

internal sealed class ActingActorResolver(IAgentRegistry agents) : IActingActorResolver
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

        // Actor names are allocated, never invented: only `agent login` and
        // the session-start hooks mint one.
        if (await agents.GetAsync(actor, cancellationToken) is null)
        {
            throw new ExitException(
                $"Unknown actor '{actor}'. Run `nitro agent login` to allocate one, "
                + "or `nitro agent list` to see the actors this workspace knows.");
        }

        return actor;
    }
}
