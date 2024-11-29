using System.Linq.Expressions;

namespace HotChocolate.Data.Filters;

/// <summary>
/// Encapsulates all filter specific information
/// </summary>
public interface IFilterContext : IFilterInfo
{
    /// <summary>
    /// Specifies the sorting execution if <paramref name="isHandled"/> is <c>false</c>.
    /// You want to enable the execution of the sorting when you do not handle the execution
    /// manually
    /// </summary>
    /// <param name="isHandled">If false, sorting is applied on the result of the resolver</param>
    void Handled(bool isHandled);

    /// <summary>
    /// Specifies if a filter was defined.
    /// </summary>
    bool IsDefined { get; }

    /// <summary>
    /// Serializes the input object to a dictionary
    /// </summary>
    IDictionary<string, object?>? ToDictionary();

    /// <summary>
    /// Creates a predicate expression for the filter context.
    /// </summary>
    Expression<Func<T, bool>>? AsPredicate<T>();
}
