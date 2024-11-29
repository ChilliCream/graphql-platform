namespace HotChocolate.Data.Sorting;

/// <summary>
/// Encapsulates all sorting specific information
/// </summary>
public interface ISortingContext
{
    /// <summary>
    /// Specifies the sorting execution if <paramref name="isHandled"/> is <c>false</c>.
    /// You want to enable the execution of the sorting when you do not handle the execution
    /// manually
    /// </summary>
    /// <param name="isHandled">If false, sorting is applied on the result of the resolver</param>
    void Handled(bool isHandled);

    /// <summary>
    /// Specifies if sorting was defined.
    /// </summary>
    bool IsDefined { get; }

    /// <summary>
    /// Specifies a delegate that is applied after sorting has been applied.
    /// </summary>
    /// <param name="action">
    /// The delegate that is applied after sorting has been applied.
    /// </param>
    /// <typeparam name="T">
    /// The type of the entity.
    /// </typeparam>
    void OnAfterSortingApplied<T>(PostSortingAction<T> action);

    /// <summary>
    /// Serializes the input object to a dictionary
    /// </summary>
    IList<IDictionary<string, object?>> ToList();

    /// <summary>
    /// Returns a collection of sorting operations in the order that they are requested
    /// </summary>
    IReadOnlyList<IReadOnlyList<ISortingFieldInfo>> GetFields();
}

public delegate TQuery PostSortingAction<TQuery>(bool userDefinedSorting, TQuery query);
