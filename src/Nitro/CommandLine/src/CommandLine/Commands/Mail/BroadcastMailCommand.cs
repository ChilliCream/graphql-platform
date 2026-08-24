using ChilliCream.Nitro.CommandLine.Commands.Agent.Options;
using ChilliCream.Nitro.CommandLine.Commands.Mail.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Notify;
using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Commands.Mail;

internal sealed class BroadcastMailCommand : Command
{
    public BroadcastMailCommand() : base("broadcast")
    {
        Description = "Send a message to every registered agent except yourself.";

        Options.Add(Opt<MailSubjectOption>.Instance);
        Options.Add(Opt<MailBodyOption>.Instance);
        Options.Add(Opt<MailBodyFileOption>.Instance);
        Options.Add(Opt<RoleAgentOption>.Instance);
        Options.Add(Opt<MailActorOption>.Instance);
        Options.Add(Opt<MailNoPingOption>.Instance);
        Options.Add(Opt<OptionalOutputFormatOption>.Instance);

        MailBody.AddValidator(this);

        this.AddExamples(
            "agent mail broadcast --subject \"Heads up\" --body \"Deploying at 5pm.\"",
            "agent mail broadcast --role \"backend\" --subject \"Heads up\" --body \"Deploying at 5pm.\"");

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static async Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken cancellationToken)
    {
        var console = services.GetRequiredService<INitroConsole>();
        var store = services.GetRequiredService<IMailStore>();
        var registry = services.GetRequiredService<IAgentRegistry>();
        var sessions = services.GetRequiredService<IAgentSessionRegistry>();
        var notifier = services.GetRequiredService<INotifier>();
        var fileSystem = services.GetRequiredService<IFileSystem>();
        var environmentVariableProvider = services.GetRequiredService<IEnvironmentVariableProvider>();
        var resultHolder = services.GetRequiredService<IResultHolder>();

        var subject = parseResult.GetRequiredValue(Opt<MailSubjectOption>.Instance);
        var role = parseResult.GetValue(Opt<RoleAgentOption>.Instance);
        var noPing = parseResult.GetValue(Opt<MailNoPingOption>.Instance);
        var actor = MailActor.Resolve(
            parseResult.GetValue(Opt<MailActorOption>.Instance), environmentVariableProvider);

        var to = role is null
            ? await ResolveEveryRegisteredAgentAsync(registry, actor, cancellationToken)
            : await MailRoleRecipients.ResolveAsync(sessions, role, actor, cancellationToken);

        if (to.Count is 0)
        {
            throw new ExitException(
                role is null
                    ? "No other registered agent to broadcast to."
                    : $"No live agent with role '{role}' to broadcast to.");
        }

        var body = await MailBody.ResolveAsync(parseResult, fileSystem, cancellationToken);

        var message = await store.SendMessageAsync(
            new MailMessageCreation
            {
                Sender = actor,
                Subject = subject,
                Body = body,
                To = to
            },
            cancellationToken);

        if (!noPing)
        {
            try
            {
                await notifier.NotifyAsync(
                    message.Recipients.Select(recipient => recipient.Name).ToArray(), cancellationToken);
            }
            catch
            {
                // A failed ping is a non-event.
            }
        }

        if (!console.IsHumanReadable)
        {
            resultHolder.SetResult(new ObjectResult(MailMessageResult.Create(message)));
            return ExitCodes.Success;
        }

        console.OkLine(
            $"Sent '{message.Id.EscapeMarkup()}' to "
            + $"{string.Join(", ", message.Recipients.Select(recipient => recipient.Name)).EscapeMarkup()}.");

        return ExitCodes.Success;
    }

    /// <summary>
    /// Returns the normalized names of every non-implicit registered agent
    /// except <paramref name="excludingActor"/>. A broadcast with no role
    /// filter reaches every registered mailbox regardless of whether any of
    /// its sessions are currently live.
    /// </summary>
    private static async Task<IReadOnlyList<string>> ResolveEveryRegisteredAgentAsync(
        IAgentRegistry registry, string excludingActor, CancellationToken cancellationToken)
    {
        var agents = await registry.ListAsync(role: null, staleBefore: null, cancellationToken);

        return agents
            .Where(agent => !agent.Implicit)
            .Select(agent => agent.Name)
            .Where(name => name != excludingActor)
            .ToArray();
    }
}
