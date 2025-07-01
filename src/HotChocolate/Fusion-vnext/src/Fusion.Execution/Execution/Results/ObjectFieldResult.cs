using System.Text.Json;
using HotChocolate.Execution;

namespace HotChocolate.Fusion.Execution;

/// <summary>
/// Represents the result of a field that returns an object.
/// </summary>
public sealed class ObjectFieldResult : FieldResult
{
    /// <summary>
    /// Gets or sets the value of the field.
    /// </summary>
    public ObjectResult? Value { get; set; }

    /// <summary>
    /// Sets the value of the field to <see langword="null"/>.
    /// </summary>
    public override void SetNextValueNull()
    {
        Value = null;
    }

    /// <summary>
    /// Sets the object result as the value of this field result.
    /// </summary>
    /// <param name="value">The value to set.</param>
    /// <exception cref="ArgumentException">
    /// The value is not a <see cref="ObjectResult"/>.
    /// </exception>
    public override void SetNextValue(ResultData value)
    {
        if (value is not ObjectResult objectResult)
        {
            throw new ArgumentException("Value is not a ObjectResult.", nameof(value));
        }

        Value = objectResult;
    }

    /// <summary>
    /// Writes the object result to a JSON writer.
    /// </summary>
    /// <param name="writer">The writer to write to.</param>
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
        if (Value is null)
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
            Value.WriteTo(writer, options, nullIgnoreCondition);
        }
    }

    /// <summary>
    /// Returns the object result as a key-value pair.
    /// </summary>
    /// <returns>
    /// A key-value pair representing the object result.
    /// </returns>
    protected internal override KeyValuePair<string, object?> AsKeyValuePair()
        => new(Selection.ResponseName, Value);

    /// <summary>
    /// Resets the object result.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> if the object result was reset; otherwise, <see langword="false"/>.
    /// </returns>
    public override bool Reset()
    {
        Value = null;
        return base.Reset();
    }
}
