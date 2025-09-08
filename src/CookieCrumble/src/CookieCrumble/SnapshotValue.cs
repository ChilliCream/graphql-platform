using System.Buffers;

namespace CookieCrumble;

public abstract class SnapshotValue : ISnapshotSegment
{
    public abstract string? Name { get; }

    public abstract ReadOnlySpan<byte> Value { get; }

    protected virtual string MarkdownType => "text";

    public virtual void FormatMarkdown(IBufferWriter<byte> snapshot)
    {
        snapshot.Append("```");
        snapshot.Append(MarkdownType);
        snapshot.AppendLine();
        snapshot.Write(Value);
        snapshot.AppendLine();
        snapshot.Append("```");
        snapshot.AppendLine();
    }
}
