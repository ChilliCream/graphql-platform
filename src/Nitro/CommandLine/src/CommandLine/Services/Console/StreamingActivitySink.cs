using ChilliCream.Nitro.CommandLine.Helpers;
using Spectre.Console.Rendering;

namespace ChilliCream.Nitro.CommandLine;

internal sealed class StreamingActivitySink : IActivitySink
{
    private readonly INitroConsole _console;
    private readonly Dictionary<ActivityEntry, EntryMeta> _meta = [];

    public StreamingActivitySink(INitroConsole console)
    {
        _console = console;
    }

    public Task Completion => Task.CompletedTask;

    public ActivityEntry AddRoot(string text)
    {
        TreeLineWriter.WriteWrapped(_console, "", "│ ", text);

        var entry = new ActivityEntry(text);
        _meta[entry] = new EntryMeta(Prefix: "", DetailsPrefix: "");
        return entry;
    }

    public ActivityEntry AddChild(ActivityEntry parent, string text, ActivityState state)
    {
        var parentMeta = _meta[parent];
        var (markupText, wrapPadding) = FormatChild(text, state);
        var linePrefix = parentMeta.Prefix + "├── ";
        var continuationPrefix = parentMeta.Prefix + "│   " + wrapPadding;

        TreeLineWriter.WriteWrapped(_console, linePrefix, continuationPrefix, markupText);

        var entry = parent.AddChild(text, state);
        var childPrefix = parentMeta.Prefix + "│   ";
        _meta[entry] = new EntryMeta(Prefix: childPrefix, DetailsPrefix: childPrefix);
        return entry;
    }

    public ActivityEntry CompleteChild(ActivityEntry parent, string text, ActivityState state)
    {
        var parentMeta = _meta[parent];
        var markupText = FormatTerminator(text, state);
        var linePrefix = parentMeta.Prefix + "└── ";
        var continuationPrefix = parentMeta.Prefix + "    " + "  ";

        TreeLineWriter.WriteWrapped(_console, linePrefix, continuationPrefix, markupText);

        var entry = parent.AddChild(text, state, isTerminator: true);
        _meta[entry] = new EntryMeta(
            Prefix: parentMeta.Prefix + "    ",
            DetailsPrefix: parentMeta.Prefix + "    ");
        return entry;
    }

    public void SetState(ActivityEntry entry, ActivityState state)
    {
        // Streaming is append-only; state changes are reflected only when
        // a terminator line is written for this entry.
    }

    public void SetTextAndState(ActivityEntry entry, string text, ActivityState state)
    {
        var entryMeta = _meta[entry];
        var markupText = FormatTerminator(text, state);
        var linePrefix = entryMeta.Prefix + "└── ";
        var continuationPrefix = entryMeta.Prefix + "    " + "  ";

        TreeLineWriter.WriteWrapped(_console, linePrefix, continuationPrefix, markupText);

        entry.Text = text;
        entry.State = state;
        _meta[entry] = entryMeta with { DetailsPrefix = entryMeta.Prefix + "    " };
    }

    public void SetDetails(ActivityEntry entry, IRenderable details)
    {
        TreeLineWriter.WriteIndented(_console, details, _meta[entry].DetailsPrefix);
        entry.Details = details;
    }

    public void FailActiveDescendants(ActivityEntry entry)
    {
        // Streaming is append-only; descendant state does not affect already-written output.
    }

    public void Stop()
    {
    }

    private static (string MarkupText, string WrapPadding) FormatChild(string text, ActivityState state)
    {
        return state switch
        {
            ActivityState.Active => (text, ""),
            ActivityState.Info => (text, ""),
            ActivityState.Warning => (Glyphs.ExclamationMark.Space() + text.AsWarning(), "  "),
            ActivityState.Waiting => (Glyphs.Clock.Space() + text, "  "),
            ActivityState.Completed => (Glyphs.Check.Space() + text, "  "),
            ActivityState.Failed => (Glyphs.Cross.Space() + text.AsError(), "  "),
            _ => (text, "")
        };
    }

    private static string FormatTerminator(string text, ActivityState state)
    {
        return state switch
        {
            ActivityState.Completed => Glyphs.Check.Space() + text,
            ActivityState.Failed => Glyphs.Cross.Space() + text.AsError(),
            ActivityState.Warning => Glyphs.ExclamationMark.Space() + text.AsWarning(),
            _ => text
        };
    }

    private readonly record struct EntryMeta(string Prefix, string DetailsPrefix);
}
