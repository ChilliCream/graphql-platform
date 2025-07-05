using System.Text.Json;
using HotChocolate.Execution;

namespace HotChocolate.Fusion.Execution;

/// <summary>
/// Represents the result of a list field in a GraphQL operation.
/// </summary>
public sealed class ListFieldResult : FieldResult
{
    /// <summary>
    /// Gets or sets the value of the list field.
    /// </summary>
    public ListResult? Value { get; set; }

    /// <summary>
    /// Sets the value of the list field to null.
    /// </summary>
    public override void SetNextValueNull()
    {
        Value = null;
    }

    /// <summary>
    /// Sets the value of the list field to the specified value.
    /// </summary>
    /// <param name="value">
    /// The value to set.
    /// </param>
    /// <exception cref="ArgumentException">
    /// <paramref name="value"/> is not a <see cref="ListResult"/>.
    /// </exception>
    public override void SetNextValue(ResultData value)
    {
        if (value is not ListResult listResult)
        {
            throw new ArgumentException("Value is not a ListResult.", nameof(value));
        }

        Value = listResult;
    }

    /// <summary>
    /// Writes the list field to the specified JSON writer.
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

    protected internal override KeyValuePair<string, object?> AsKeyValuePair()
    {
        throw new NotImplementedException();
    }

    public override bool Reset()
    {
        Value = null;
        return base.Reset();
    }
}
