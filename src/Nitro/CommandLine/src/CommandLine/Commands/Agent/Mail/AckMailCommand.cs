using ChilliCream.Nitro.CommandLine.Commands.Agent.Mail.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Mail;

internal sealed class AckMailCommand : Command
{
    public AckMailCommand() : base("ack")
    {
        Description = "Mark one or more messages read without printing them.";

        Arguments.Add(Opt<MailMessageIdsArgument>.Instance);

        Options.Add(Opt<MailActorOption>.Instance);
        Options.Add(Opt<OptionalOutputFormatOption>.Instance);

        this.AddExamples(
            "agent mail ack \"m-abc123\"",
            "agent mail ack \"m-abc123\" \"m-def456\"");

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static async Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken cancellationToken)
    {
        var console = services.GetRequiredService<INitroConsole>();
        var store = services.GetRequiredService<IMailStore>();
        var actorResolver = services.GetRequiredService<IActingActorResolver>();
        var resultHolder = services.GetRequiredService<IResultHolder>();

        var ids = parseResult.GetRequiredValue(Opt<MailMessageIdsArgument>.Instance)
            .Distinct()
            .ToArray();
        var actor = await MailActor.ResolveAsync(
            parseResult.GetValue(Opt<MailActorOption>.Instance), actorResolver, cancellationToken);

        await store.MarkReadAsync(ids, actor, cancellationToken);

        if (!console.IsHumanReadable)
        {
            resultHolder.SetResult(new ObjectResult(new MailIdsResult(ids)));
            return ExitCodes.Success;
        }

        foreach (var id in ids)
        {
            console.OkLine($"Marked '{id.EscapeMarkup()}' read.");
        }

        return ExitCodes.Success;
    }
}
