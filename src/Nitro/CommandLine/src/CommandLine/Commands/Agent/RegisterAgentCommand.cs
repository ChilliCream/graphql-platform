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
        var agents = services.GetRequiredService<IAgentRegistry>();

        var actor = MailAgentName.Normalize(
            parseResult.GetValue(Opt<RequiredActorOption>.Instance) ?? string.Empty);
        var role = parseResult.GetValue(Opt<RoleAgentOption>.Instance) ?? string.Empty;

        // Actor names are allocated, never invented: only `agent login` and
        // the session-start hooks mint one.
        if (await agents.GetAsync(actor, cancellationToken) is null)
        {
            throw new ExitException(
                $"Unknown actor '{actor}'. Run `nitro agent login` to allocate one, "
                + "or `nitro agent list` to see the actors this workspace knows.");
        }

        var registered = await agents.RegisterAsync(actor, role, client: string.Empty, cancellationToken);

        if (!console.IsHumanReadable)
        {
            resultHolder.SetResult(new ObjectResult(new AgentRegisterResult(registered.Name, registered.Role)));

            return ExitCodes.Success;
        }

        console.OkLine(
            registered.Role.Length > 0
                ? $"Actor '{registered.Name.EscapeMarkup()}', role '{registered.Role.EscapeMarkup()}'."
                : $"Actor '{registered.Name.EscapeMarkup()}'.");

        return ExitCodes.Success;
    }

    public sealed record AgentRegisterResult(string Actor, string Role);
}
