using System.Buffers;

namespace CookieCrumble.Formatters;

internal sealed class PlainTextSnapshotValueFormatter(
    string markdownLanguage = MarkdownLanguages.Text)
    : ISnapshotValueFormatter
    , IMarkdownSnapshotValueFormatter
{
    public bool CanHandle(object? value)
        => value is string;

    public void Format(IBufferWriter<byte> snapshot, object? value)
    {
        if (value?.ToString() is { } s)
        {
            snapshot.Append(s);
        }
    }

    public void FormatMarkdown(IBufferWriter<byte> snapshot, object? value)
    {
        snapshot.Append($"```{markdownLanguage}");
        snapshot.AppendLine();
        Format(snapshot, value);
        snapshot.AppendLine();
        snapshot.Append("```");
        snapshot.AppendLine();
    }
}
