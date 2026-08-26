using ChilliCream.Nitro.CommandLine.Commands.Agent.Memory.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services.Memory;

namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Memory;

internal sealed class ContextMemoryCommand : Command
{
    public ContextMemoryCommand() : base("context")
    {
        Description = "Assemble curated memories into a prompt-ready block within a character budget.";

        Options.Add(Opt<MemoryTagOption>.Instance);
        Options.Add(Opt<MemoryContextLimitOption>.Instance);
        Options.Add(Opt<MemoryMaxCharsOption>.Instance);
        Options.Add(Opt<OptionalOutputFormatOption>.Instance);

        this.AddExamples(
            "agent memory context",
            "agent memory context --tag onboarding",
            "agent memory context --max-chars 4000");

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static async Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken cancellationToken)
    {
        var console = services.GetRequiredService<INitroConsole>();
        var store = services.GetRequiredService<IMemoryStore>();
        var resultHolder = services.GetRequiredService<IResultHolder>();

        var tags = (parseResult.GetValue(Opt<MemoryTagOption>.Instance) ?? [])
            .Select(MemoryTags.Normalize)
            .ToArray();
        var limit = parseResult.GetRequiredValue(Opt<MemoryContextLimitOption>.Instance);
        var maxChars = parseResult.GetRequiredValue(Opt<MemoryMaxCharsOption>.Instance);

        // Context is curated only, never journal, and always reads the
        // merged project-then-global candidate order the budget algorithm
        // ranks against.
        IReadOnlyList<MemoryRecord> candidates;

        candidates = await store.GetRecentCuratedAsync(null, cancellationToken);

        if (tags.Length > 0)
        {
            candidates = candidates
                .Where(record => tags.All(tag => record.Tags.Contains(tag, StringComparer.Ordinal)))
                .ToList();
        }

        var selection = MemoryContextBudget.Select(candidates, limit, maxChars);

        if (!console.IsHumanReadable)
        {
            resultHolder.SetResult(new ObjectResult(MemoryContextResult.Create(selection)));
            return ExitCodes.Success;
        }

        if (selection.OmittedEntryId is { } omittedId)
        {
            console.WriteLine(
                $"Memory '{omittedId}' alone exceeds --max-chars ({maxChars}); no entries returned.");
            return ExitCodes.Success;
        }

        if (selection.Entries.Count == 0)
        {
            console.WriteLine("No memories found.");
            return ExitCodes.Success;
        }

        console.WriteLine(MemoryContextRenderer.Render(selection.Entries));

        return ExitCodes.Success;
    }
}
