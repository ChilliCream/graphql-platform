using System.Text.Json;
using HotChocolate.Execution;

namespace HotChocolate.Fusion.Execution;

/// <summary>
/// Represents the result of a leaf field (a scalar or enum field) in a GraphQL operation.
/// </summary>
public sealed class LeafFieldResult : FieldResult
{
    /// <summary>
    /// Gets or sets the value of the leaf field.
    /// </summary>
    public JsonElement Value { get; set; }

    /// <summary>
    /// Sets the value of the leaf field to null.
    /// </summary>
    public override void SetNextValueNull()
        => Value = default;

    /// <summary>
    /// Sets the value of the leaf field to the specified value.
    /// </summary>
    /// <param name="value">
    /// The value to set.
    /// </param>
    public override void SetNextValue(JsonElement value)
        => Value = value;

    /// <summary>
    /// Writes the result of the leaf field to the specified JSON writer.
    /// </summary>
    /// <param name="writer">
    /// The JSON writer to write the result to.
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
        if (Value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            if ((nullIgnoreCondition & JsonNullIgnoreCondition.Fields) == JsonNullIgnoreCondition.Fields)
            {
                return;
            }

            writer.WritePropertyName(Selection.ResponseName);
            writer.WriteNullValue();
        }
        else
        {
            writer.WritePropertyName(Selection.ResponseName);
            Value.WriteTo(writer);
        }
    }

    /// <inheritdoc />
    public override bool HasNullValue => Value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined;

    /// <inheritdoc />
    protected internal override KeyValuePair<string, object?> AsKeyValuePair()
        => new(Selection.ResponseName, Value);

    public override bool Reset()
    {
        Value = default;
        return base.Reset();
    }
}
