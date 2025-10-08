using System.Text.Json;
using HotChocolate.Buffers;
using HotChocolate.Execution;

namespace HotChocolate.Fusion.Execution.Results;

public sealed class RawListFieldResult : ListResult
{
    private readonly List<ReadOnlyMemorySegment?> _items = [];

    public override int Capacity
    {
        get => _items.Capacity;
        protected set => _items.Capacity = value;
    }

    public override void SetNextValueNull()
        => _items.Add(null);

    public override void SetNextValue(ReadOnlyMemorySegment value)
    {
        _items.Add(value);
    }

    public override void WriteTo(
        Utf8JsonWriter writer,
        JsonSerializerOptions? options = null,
        JsonNullIgnoreCondition nullIgnoreCondition = JsonNullIgnoreCondition.None)
    {
        writer.WriteStartArray();
        foreach (var item in _items)
        {
            if (!item.HasValue)
            {
                writer.WriteNullValue();
                continue;
            }

            var span = item.Value.Span;

            if (span.Length == 0)
            {
                writer.WriteNullValue();
                continue;
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
        writer.WriteEndArray();
    }

    public override bool Reset()
    {
        _items.Clear();
        return base.Reset();
    }
}
