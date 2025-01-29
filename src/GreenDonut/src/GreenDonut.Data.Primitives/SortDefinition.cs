using System.Collections.Immutable;
using System.Linq.Expressions;
using System.Text;

namespace GreenDonut.Data;

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
    /// <param name="operations">
    /// The sort operations.
    /// </param>
    public SortDefinition(params ISortBy<T>[] operations)
    {
        Operations = [..operations];
    }

    /// <summary>
    /// Initializes a new instance of <see cref="SortDefinition{T}"/>.
    /// </summary>
    /// <param name="operations">
    /// The sort operations.
    /// </param>
    public SortDefinition(IEnumerable<ISortBy<T>> operations)
    {
        Operations = [..operations];
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

    /// <summary>
    /// Adds a sort operation to the definition.
    /// </summary>
    /// <param name="keySelector">
    /// The field on which the sort operation is applied.
    /// </param>
    /// <typeparam name="TResult">
    /// The type of the field on which the sort operation is applied.
    /// </typeparam>
    /// <returns>
    /// The updated sort definition.
    /// </returns>
    public SortDefinition<T> AddAscending<TResult>(
        Expression<Func<T, TResult>> keySelector)
    {
        if (keySelector == null)
        {
            throw new ArgumentNullException(nameof(keySelector));
        }

        var operations = Operations.Add(SortBy<T>.Ascending(keySelector));
        return new SortDefinition<T>(operations);
    }

    /// <summary>
    /// Adds a descending sort operation to the definition.
    /// </summary>
    /// <param name="keySelector">
    /// The field on which the sort operation is applied.
    /// </param>
    /// <typeparam name="TResult">
    /// The type of the field on which the sort operation is applied.
    /// </typeparam>
    /// <returns>
    /// The updated sort definition.
    /// </returns>
    public SortDefinition<T> AddDescending<TResult>(
        Expression<Func<T, TResult>> keySelector)
    {
        if (keySelector == null)
        {
            throw new ArgumentNullException(nameof(keySelector));
        }

        var operations = Operations.Add(SortBy<T>.Ascending(keySelector));
        return new SortDefinition<T>(operations);
    }

    /// <summary>
    /// Returns a string representation of the sort definition.
    /// </summary>
    /// <returns>
    /// A string representation of the sort definition.
    /// </returns>
    public override string ToString()
    {
        if (Operations.Length == 0)
        {
            return "{}";
        }

        var next = false;
        var sb = new StringBuilder();
        sb.Append('{');

        foreach (var operation in Operations)
        {
            if (next)
            {
                sb.Append(',');
            }

            sb.Append(operation.KeySelector);
            sb.Append(':');
            sb.Append(operation.Ascending ? "ASC" : "DESC");
            next = true;
        }

        sb.Append('}');
        return sb.ToString();
    }

    public static SortDefinition<T> Empty { get; } = new();
}
