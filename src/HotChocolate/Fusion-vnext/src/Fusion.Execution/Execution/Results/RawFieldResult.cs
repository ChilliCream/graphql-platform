using System.Text.Json;
using HotChocolate.Buffers;
using HotChocolate.Execution;

namespace HotChocolate.Fusion.Execution;

public sealed class RawFieldResult : FieldResult
{
    public override bool HasNullValue => Value.Length == 0;

    public ReadOnlyMemorySegment Value { get; private set; }

    public override void SetNextValue(ReadOnlyMemorySegment value) => Value = value;

    public override void WriteTo(
        Utf8JsonWriter writer,
        JsonSerializerOptions? options = null,
        JsonNullIgnoreCondition nullIgnoreCondition = JsonNullIgnoreCondition.None)
    {
        writer.WritePropertyName(Selection.ResponseName);

        var span = Value.Span;

        if (span.Length == 0)
        {
            writer.WriteNullValue();
        }

        switch (span[0])
        {
            case RawFieldValueType.String:
                writer.WriteStringValue(span[1..]);
                break;

            case RawFieldValueType.Boolean when span[1] == 0:
                writer.WriteBooleanValue(false);
                break;

            case RawFieldValueType.Boolean when span[1] == 1:
                writer.WriteBooleanValue(true);
                break;
        }
    }

    protected internal override KeyValuePair<string, object?> AsKeyValuePair()
        => new(Selection.ResponseName, Value);

    public override bool Reset()
    {
        Value = default;
        return base.Reset();
    }
}
