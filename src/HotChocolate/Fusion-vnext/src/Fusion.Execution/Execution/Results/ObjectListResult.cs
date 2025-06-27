using System.Text.Json;
using HotChocolate.Execution;

namespace HotChocolate.Fusion.Execution;

/// <summary>
/// Represents list result of object values.
/// </summary>
public sealed class ObjectListResult : ListResult
{
    /// <summary>
    /// Gets the items of the object list result.
    /// </summary>
    public List<ObjectResult?> Items { get; } = [];

    /// <summary>
    /// Adds a null value to the list.
    /// </summary>
    public override void SetNextValueNull()
    {
        Items.Add(null);
    }

    /// <summary>
    /// Adds the given value to the list.
    /// </summary>
    /// <param name="value">The value to add.</param>
    /// <exception cref="ArgumentException">
    /// The value is not a <see cref="ObjectResult"/>.
    /// </exception>
    public override void SetNextValue(ResultData value)
    {
        if (value is not ObjectResult objectResult)
        {
            throw new ArgumentException("Value is not a ObjectResult.", nameof(value));
        }

        Items.Add(objectResult);
    }

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
            if (item is null)
            {
                if ((nullIgnoreCondition & JsonNullIgnoreCondition.Lists) == JsonNullIgnoreCondition.Lists)
                {
                    continue;
                }

                writer.WriteNullValue();
            }
            else
            {
                item.WriteTo(writer, options, nullIgnoreCondition);
            }
        }

        writer.WriteEndArray();
    }

    /// <summary>
    /// Resets the list.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> if the list was reset; otherwise, <see langword="false"/>.
    /// </returns>
    public override bool Reset()
    {
        Items.Clear();

        if (Items.Capacity > 512)
        {
            Items.Capacity = 512;
        }

        return base.Reset();
    }
}
