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
        Options.Add(Opt<ClientAgentOption>.Instance);
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
        var client = parseResult.GetValue(Opt<ClientAgentOption>.Instance)
            ?? DetectClient(environmentVariableProvider)
            ?? "";

        var agent = await registry.RegisterAsync(actor, role, client, cancellationToken);

        if (!console.IsHumanReadable)
        {
            resultHolder.SetResult(
                new ObjectResult(
                    new AgentRegisterResult(
                        agent.Name, agent.Role, agent.Client, agent.RegisteredAt, agent.LastSeenAt)));
            return ExitCodes.Success;
        }

        console.OkLine(
            agent.Role.Length > 0
                ? $"Registered '{agent.Name.EscapeMarkup()}' as '{agent.Role.EscapeMarkup()}'."
                : $"Registered '{agent.Name.EscapeMarkup()}'.");

        return ExitCodes.Success;
    }

    /// <summary>
    /// Detects the CLI's client program from environment markers, used as
    /// the fallback when <c>--client</c> is not given. Only markers
    /// confirmed present in a real session of the corresponding tool are
    /// checked; an unconfirmed tool is left undetected rather than guessed,
    /// so its identity can only be recorded via <c>--client</c>.
    ///
    /// | Marker present | Detected as   |
    /// |-----------------|---------------|
    /// | <c>CLAUDECODE</c> | <c>claude-code</c> |
    /// </summary>
    internal static string? DetectClient(IEnvironmentVariableProvider environmentVariables)
        => environmentVariables.GetEnvironmentVariable("CLAUDECODE") is not null
            ? "claude-code"
            : null;

    public sealed record AgentRegisterResult(
        string Name, string Role, string Client, DateTimeOffset RegisteredAt, DateTimeOffset LastSeenAt);
}
