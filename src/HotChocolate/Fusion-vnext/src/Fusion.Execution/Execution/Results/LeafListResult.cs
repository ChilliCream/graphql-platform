using System.Text.Json;
using HotChocolate.Execution;

namespace HotChocolate.Fusion.Execution;

/// <summary>
/// Represents list result of leaf values.
/// </summary>
public sealed class LeafListResult : ListResult
{
    /// <summary>
    /// Gets the items of the leaf list result.
    /// </summary>
    public List<JsonElement> Items { get; } = [];

    /// <summary>
    /// Gets the capacity of the leaf list result.
    /// </summary>
    public override int Capacity
    {
        get => Items.Capacity;
        protected set => Items.Capacity = value;
    }

    /// <summary>
    /// Adds a null value to the list.
    /// </summary>
    public override void SetNextValueNull()
        => Items.Add(default);

    /// <summary>
    /// Adds the given value to the list.
    /// </summary>
    /// <param name="value">
    /// The value to add.
    /// </param>
    public override void SetNextValue(JsonElement value)
        => Items.Add(value);

    /// <summary>
    /// Writes the list to the specified JSON writer.
    /// </summary>
    /// <param name="writer">
    /// The JSON writer to write the list to.
    /// </param>
    /// <param name="options">
    /// The serializer options.
    /// If options are set to null <see cref="JsonSerializerOptions"/>.Web will be used.
    /// </param>
    /// <param name="nullIgnoreCondition">
    /// The null ignore condition.
    /// </param>
    public override void WriteTo(
        Utf8JsonWriter writer,
        JsonSerializerOptions? options = null,
        JsonNullIgnoreCondition nullIgnoreCondition = JsonNullIgnoreCondition.None)
    {
        writer.WriteStartArray();
        foreach (var item in Items)
        {
            if (item.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            {
                if ((nullIgnoreCondition & JsonNullIgnoreCondition.Lists) == JsonNullIgnoreCondition.Lists)
                {
                    continue;
                }

                writer.WriteNullValue();
            }
            else
            {
                item.WriteTo(writer);
            }
        }
        writer.WriteEndArray();
    }

    public override bool Reset()
    {
        Items.Clear();
        return base.Reset();
    }
}
