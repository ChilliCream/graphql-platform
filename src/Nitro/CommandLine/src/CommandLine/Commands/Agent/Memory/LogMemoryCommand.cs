using ChilliCream.Nitro.CommandLine.Commands.Agent.Memory.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Memory;
using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Memory;

internal sealed class LogMemoryCommand : Command
{
    public LogMemoryCommand() : base("log")
    {
        Description = "Capture a cheap journal entry. No type or tags at capture time; "
            + "assign those when promoting.";

        Arguments.Add(Opt<MemoryTextArgument>.Instance);

        Options.Add(Opt<MemoryFileOption>.Instance);
        Options.Add(Opt<MemoryActorOption>.Instance);
        Options.Add(Opt<MemoryWriteScopeOption>.Instance);
        Options.Add(Opt<OptionalOutputFormatOption>.Instance);

        MemoryBody.AddValidator(this);

        this.AddExamples(
            "agent memory log \"Investigated the flaky test; still unresolved.\"",
            "agent memory log --file session-notes.md");

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static async Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken cancellationToken)
    {
        var console = services.GetRequiredService<INitroConsole>();
        var store = services.GetRequiredService<IMemoryStore>();
        var fileSystem = services.GetRequiredService<IFileSystem>();
        var actorResolver = services.GetRequiredService<IActingActorResolver>();
        var resultHolder = services.GetRequiredService<IResultHolder>();

        var text = await MemoryBody.ResolveAsync(parseResult, fileSystem, cancellationToken);
        var actor = await MemoryActor.ResolveAsync(
            parseResult.GetValue(Opt<MemoryActorOption>.Instance), actorResolver, cancellationToken);
        var scope = parseResult.GetRequiredValue(Opt<MemoryWriteScopeOption>.Instance);

        var entry = await store.LogAsync(
            new MemoryJournalEntryCreation
            {
                Text = text,
                Actor = actor,
                Scope = scope
            },
            cancellationToken);

        if (!console.IsHumanReadable)
        {
            resultHolder.SetResult(new ObjectResult(MemoryJournalEntryResult.Create(entry)));
            return ExitCodes.Success;
        }

        console.OkLine($"Logged journal entry '{entry.Id.EscapeMarkup()}'.");

        return ExitCodes.Success;
    }
}
