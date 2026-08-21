using ChilliCream.Nitro.CommandLine.Commands.Agent.Options;
using ChilliCream.Nitro.CommandLine.Commands.Mail.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Commands.Agent;

internal sealed class RegisterAgentCommand : Command
{
    public RegisterAgentCommand() : base("register")
    {
        Description = "Register the resolved actor as an agent, with an optional role. "
            + "--actor is per invocation; set NITRO_MAIL_ACTOR to persist an identity.";

        Options.Add(Opt<MailActorOption>.Instance);
        Options.Add(Opt<RoleAgentOption>.Instance);
        Options.Add(Opt<OptionalOutputFormatOption>.Instance);

        this.AddExamples("agent register", "agent register --role \"backend\"");

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static async Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken cancellationToken)
    {
        var console = services.GetRequiredService<INitroConsole>();
        var registry = services.GetRequiredService<IAgentRegistry>();
        var environmentVariableProvider = services.GetRequiredService<IEnvironmentVariableProvider>();
        var resultHolder = services.GetRequiredService<IResultHolder>();

        var actor = MailActor.Resolve(
            parseResult.GetValue(Opt<MailActorOption>.Instance), environmentVariableProvider);
        var role = parseResult.GetValue(Opt<RoleAgentOption>.Instance) ?? "";

        var agent = await registry.RegisterAsync(actor, role, cancellationToken);

        if (!console.IsHumanReadable)
        {
            resultHolder.SetResult(
                new ObjectResult(
                    new AgentRegisterResult(agent.Name, agent.Role, agent.RegisteredAt, agent.LastSeenAt)));
            return ExitCodes.Success;
        }

        console.OkLine(
            agent.Role.Length > 0
                ? $"Registered '{agent.Name.EscapeMarkup()}' as '{agent.Role.EscapeMarkup()}'."
                : $"Registered '{agent.Name.EscapeMarkup()}'.");

        return ExitCodes.Success;
    }

    public sealed record AgentRegisterResult(
        string Name, string Role, DateTimeOffset RegisteredAt, DateTimeOffset LastSeenAt);
}
