using System.Text.Json;
using HotChocolate.Execution;

namespace HotChocolate.Fusion.Execution;

/// <summary>
/// Represents the result of a nested list field in a GraphQL operation.
/// </summary>
public sealed class NestedListResult : ListResult
{
    /// <summary>
    /// Gets the items of lists that are nested in this list.
    /// </summary>
    public List<ListResult?> Items { get; } = [];

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
    {
        Items.Add(null);
    }

    /// <summary>
    /// Adds a list to this list.
    /// </summary>
    /// <param name="value">The list result to add.</param>
    /// <exception cref="ArgumentException">
    /// The value is not a <see cref="ListResult"/>.
    /// </exception>
    public override void SetNextValue(ResultData value)
    {
        if (value is not ListResult listResult)
        {
            throw new ArgumentException("Value is not a ListResult.", nameof(value));
        }

        listResult.SetParent(this, Items.Count);
        Items.Add(listResult);
    }

    /// <summary>
    /// Writes the nested list to the specified JSON writer.
    /// </summary>
    /// <param name="writer">
    /// The JSON writer to write the nested list to.
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
        return base.Reset();
    }
}
