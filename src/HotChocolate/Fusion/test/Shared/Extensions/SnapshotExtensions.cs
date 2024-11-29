using System.Text;
using HotChocolate.Skimmed;
using HotChocolate.Skimmed.Serialization;

namespace HotChocolate.Fusion;

public static class SnapshotExtensions
{
    public static void MatchSnapshot(
        this SchemaDefinition value,
        string? postFix = null,
        string? extension = null)
    {
        Snapshot
            .Create(postFix, extension)
            .Add(new SchemaSnapshotValue(value))
            .Match();
    }

    private sealed class SchemaSnapshotValue(SchemaDefinition schema) : SnapshotValue
    {
        private readonly byte[] _value = Encoding.UTF8.GetBytes(SchemaFormatter.FormatAsString(schema));

        public override string? Name => "Schema";

        public override ReadOnlySpan<byte> Value => _value;
    }
}
