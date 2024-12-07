using System.Buffers;
using System.Runtime.CompilerServices;
using CookieCrumble.Formatters;
using HotChocolate.Fusion.Planning.Nodes;
using HotChocolate.Language;
using HotChocolate.Language.Utilities;

namespace HotChocolate.Fusion;

internal static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Initialize()
    {
        CookieCrumbleTUnit.Initialize();
        Snapshot.TryRegisterFormatter(new RootPlanNodeSnapshotValueFormatter());
        Snapshot.TryRegisterFormatter(new SyntaxNodeSnapshotValueFormatter());
    }

    private sealed class RootPlanNodeSnapshotValueFormatter() : SnapshotValueFormatter<RootPlanNode>("json")
    {
        protected override void Format(IBufferWriter<byte> snapshot, RootPlanNode value)
            => value.Serialize(snapshot);
    }

    private sealed class SyntaxNodeSnapshotValueFormatter : SnapshotValueFormatter<ISyntaxNode>
    {
        protected override void Format(IBufferWriter<byte> snapshot, ISyntaxNode value)
        {
            var serialized = value.Print().AsSpan();
            var buffer = ArrayPool<char>.Shared.Rent(serialized.Length);
            var span = buffer.AsSpan()[..serialized.Length];
            var written = 0;

            for (var i = 0; i < serialized.Length; i++)
            {
                if (serialized[i] is not '\r')
                {
                    span[written++] = serialized[i];
                }
            }

            span = span[..written];
            snapshot.Append(span);

            ArrayPool<char>.Shared.Return(buffer);
        }

        protected override void FormatMarkdown(IBufferWriter<byte> snapshot, ISyntaxNode value)
        {
            snapshot.Append("```graphql");
            snapshot.AppendLine();
            Format(snapshot, value);
            snapshot.AppendLine();
            snapshot.Append("```");
            snapshot.AppendLine();
        }
    }
}
