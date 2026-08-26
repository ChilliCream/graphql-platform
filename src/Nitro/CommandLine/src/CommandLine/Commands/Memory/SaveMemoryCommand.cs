using ChilliCream.Nitro.CommandLine.Commands.Memory.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Memory;
using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Commands.Memory;

internal sealed class SaveMemoryCommand : Command
{
    public SaveMemoryCommand() : base("save")
    {
        Description = "Save a curated memory.";

        Arguments.Add(Opt<MemoryTextArgument>.Instance);

        Options.Add(Opt<MemoryFileOption>.Instance);
        Options.Add(Opt<MemoryTypeOption>.Instance);
        Options.Add(Opt<MemoryTagOption>.Instance);
        Options.Add(Opt<MemoryActorOption>.Instance);
        Options.Add(Opt<MemoryWriteScopeOption>.Instance);
        Options.Add(Opt<OptionalOutputFormatOption>.Instance);

        MemoryBody.AddValidator(this);

        // --type has no sensible default (unlike a task's type), so it is
        // required here even though the shared option instance also serves
        // `update`, where it is optional.
        Validators.Add(result =>
        {
            if (result.GetResult(Opt<MemoryTypeOption>.Instance) is not { Implicit: false })
            {
                result.AddError("Option '--type' is required.");
            }
        });

        this.AddExamples(
            "agent memory save \"Use pnpm, not npm, in this repo.\" --type preference",
            "agent memory save --file notes.md --type fact --tag build");

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
        var type = parseResult.GetRequiredValue(Opt<MemoryTypeOption>.Instance);
        var tags = parseResult.GetValue(Opt<MemoryTagOption>.Instance) ?? [];
        var actor = await MemoryActor.ResolveAsync(
            parseResult.GetValue(Opt<MemoryActorOption>.Instance), actorResolver, cancellationToken);
        var scope = parseResult.GetRequiredValue(Opt<MemoryWriteScopeOption>.Instance);

        var record = await store.SaveAsync(
            new MemoryRecordCreation
            {
                Text = text,
                Type = type,
                Tags = tags,
                Actor = actor,
                Scope = scope
            },
            cancellationToken);

        if (!console.IsHumanReadable)
        {
            resultHolder.SetResult(new ObjectResult(MemoryRecordResult.Create(record)));
            return ExitCodes.Success;
        }

        console.OkLine($"Saved memory '{record.Id.EscapeMarkup()}'.");

        return ExitCodes.Success;
    }
}
