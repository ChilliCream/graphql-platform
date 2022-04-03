using System.Collections.Generic;

namespace HotChocolate.Data.Sorting;

/// <summary>
/// Encapuslates all sorting specific information
/// </summary>
public interface ISortingContext
{
    /// <summary>
    /// Enables the sorting exceution if <paramref name="enable"/> is <c>true</c>.
    /// You want to enable the execution of the sorting when you do not handle the execution
    /// manually
    /// </summary>
    /// <param name="enable">If true, sorting is applied on the result of the resolver</param>
    void EnableSortingExecution(bool enable = true);

    /// <summary>
    /// Serializes the input object to a dictionary
    /// </summary>
    IList<IDictionary<string, object?>> ToList();

    /// <summary>
    /// Returns a collection of sorting operations in the order that they are requested
    /// </summary>
    IReadOnlyList<IReadOnlyList<ISortingFieldInfo>> GetFields();
}
