using ChilliCream.Nitro.CommandLine.Commands.Agent.Memory.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Memory;

namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Memory;

internal sealed class UpdateMemoryCommand : Command
{
    public UpdateMemoryCommand() : base("update")
    {
        Description = "Update a curated memory's fields.";

        Arguments.Add(Opt<MemoryIdArgument>.Instance);

        Options.Add(Opt<MemoryTextOption>.Instance);
        Options.Add(Opt<MemoryFileOption>.Instance);
        Options.Add(Opt<MemoryTypeOption>.Instance);
        Options.Add(Opt<MemoryAddTagOption>.Instance);
        Options.Add(Opt<MemoryRemoveTagOption>.Instance);
        Options.Add(Opt<OptionalOutputFormatOption>.Instance);

        Validators.Add(result =>
        {
            var textGiven = result.GetResult(Opt<MemoryTextOption>.Instance) is { Implicit: false };
            var fileGiven = result.GetResult(Opt<MemoryFileOption>.Instance) is { Implicit: false };

            if (textGiven && fileGiven)
            {
                result.AddError("At most one of '--text' or '--file' may be given.");
            }
        });

        this.AddExamples(
            "agent memory update \"01hqzxk8xdtd3fk3f0z7c5g8vm\" --type decision",
            "agent memory update \"01hqzxk8xdtd3fk3f0z7c5g8vm\" --add-tag api --remove-tag draft");

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
        var resultHolder = services.GetRequiredService<IResultHolder>();

        var id = parseResult.GetRequiredValue(Opt<MemoryIdArgument>.Instance);

        var textGiven = parseResult.GetResult(Opt<MemoryTextOption>.Instance) is { Implicit: false };
        var fileGiven = parseResult.GetResult(Opt<MemoryFileOption>.Instance) is { Implicit: false };
        var typeGiven = parseResult.GetResult(Opt<MemoryTypeOption>.Instance) is { Implicit: false };
        var addTagsGiven = parseResult.GetResult(Opt<MemoryAddTagOption>.Instance) is { Implicit: false };
        var removeTagsGiven = parseResult.GetResult(Opt<MemoryRemoveTagOption>.Instance) is { Implicit: false };

        if (!textGiven && !fileGiven && !typeGiven && !addTagsGiven && !removeTagsGiven)
        {
            throw new ExitException("Nothing to update. Pass at least one option.");
        }

        string? text = null;

        if (textGiven)
        {
            text = parseResult.GetValue(Opt<MemoryTextOption>.Instance) ?? "";

            if (text.Length is 0)
            {
                throw new ExitException("The '--text' option must not be empty.");
            }
        }
        else if (fileGiven)
        {
            var file = parseResult.GetRequiredValue(Opt<MemoryFileOption>.Instance);
            text = await MemoryBody.ReadFileAsync(fileSystem, file, cancellationToken);
        }

        var record = await store.UpdateAsync(
            id,
            new MemoryRecordUpdate
            {
                Text = text,
                TextGiven = textGiven || fileGiven,
                Type = typeGiven ? parseResult.GetValue(Opt<MemoryTypeOption>.Instance) : null,
                TypeGiven = typeGiven,
                AddTags = parseResult.GetValue(Opt<MemoryAddTagOption>.Instance) ?? [],
                RemoveTags = parseResult.GetValue(Opt<MemoryRemoveTagOption>.Instance) ?? []
            },
            cancellationToken);

        if (!console.IsHumanReadable)
        {
            resultHolder.SetResult(new ObjectResult(MemoryRecordResult.Create(record)));
            return ExitCodes.Success;
        }

        console.OkLine($"Updated memory '{record.Id.EscapeMarkup()}'.");

        return ExitCodes.Success;
    }
}
