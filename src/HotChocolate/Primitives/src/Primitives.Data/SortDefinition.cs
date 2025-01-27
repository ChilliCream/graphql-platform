using System.Collections.Immutable;

namespace HotChocolate.Data;

/// <summary>
/// Represents all sort operations applied to an entity type.
/// </summary>
/// <typeparam name="T">
/// The entity type on which the sort operations are applied.
/// </typeparam>
public sealed record SortDefinition<T>
{
    /// <summary>
    /// Initializes a new instance of <see cref="SortDefinition{T}"/>.
    /// </summary>
    /// <param name="items">
    /// The sort operations.
    /// </param>
    public SortDefinition(params ISortBy<T>[] items)
    {
        Operations = [..items];
    }

    /// <summary>
    /// Initializes a new instance of <see cref="SortDefinition{T}"/>.
    /// </summary>
    /// <param name="items">
    /// The sort operations.
    /// </param>
    public SortDefinition(IEnumerable<ISortBy<T>> items)
    {
        Operations = [..items];
    }

    /// <summary>
    /// The sort operations.
    /// </summary>
    public ImmutableArray<ISortBy<T>> Operations { get; init; }

    /// <summary>
    /// Deconstructs the sort operations.
    /// </summary>
    /// <param name="operations">
    /// The sort operations.
    /// </param>
    public void Deconstruct(out ImmutableArray<ISortBy<T>> operations)
        => operations = Operations;
}
