using ChilliCream.Nitro.CommandLine.Commands.Agent.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services.Workspace;
using ChilliCream.Nitro.CommandLine.Services.Mail;

namespace ChilliCream.Nitro.CommandLine.Commands.Agent;

internal sealed class RegisterAgentCommand : Command
{
    public RegisterAgentCommand() : base("register")
    {
        Description = "Set the role of an actor allocated by `agent login` or a session-start hook.";

        Options.Add(Opt<RequiredActorOption>.Instance);
        Options.Add(Opt<RoleAgentOption>.Instance);
        Options.Add(Opt<OptionalOutputFormatOption>.Instance);

        this.AddExamples("agent register --actor \"maya\"", "agent register --actor \"maya\" --role \"researcher\"");

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static async Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken cancellationToken)
    {
        var console = services.GetRequiredService<INitroConsole>();
        var resultHolder = services.GetRequiredService<IResultHolder>();
        var agents = services.GetRequiredService<IAgentStore>();

        var actor = MailAgentName.Normalize(
            parseResult.GetValue(Opt<RequiredActorOption>.Instance) ?? string.Empty);
        var roleGiven = parseResult.GetResult(Opt<RoleAgentOption>.Instance) is { Implicit: false };
        var role = parseResult.GetValue(Opt<RoleAgentOption>.Instance) ?? string.Empty;

        // Actor names are allocated, never invented: only `agent login` and
        // the session-start hooks mint one.
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

        string registeredName;
        string registeredRole;

        if (roleGiven)
        {
            var registered = await agents.SetRoleAsync(actor, role, cancellationToken)
                ?? throw new ExitException($"Agent '{actor}' was deleted.");

            registeredName = registered.Name;
            registeredRole = registered.Role;
        }
        else
        {
            // Nothing else on the row changes: touch the beat and report the role as is.
            await agents.TouchAsync(actor, cancellationToken);

            registeredName = existing.Name;
            registeredRole = existing.Role;
        }

        if (!console.IsHumanReadable)
        {
            resultHolder.SetResult(new ObjectResult(new AgentRegisterResult(registeredName, registeredRole)));

            return ExitCodes.Success;
        }

        console.OkLine(
            registeredRole.Length > 0
                ? $"Actor '{registeredName.EscapeMarkup()}', role '{registeredRole.EscapeMarkup()}'."
                : $"Actor '{registeredName.EscapeMarkup()}'.");

        return ExitCodes.Success;
    }

    public sealed record AgentRegisterResult(string Actor, string Role);
}
