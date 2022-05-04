using System.Collections.Generic;

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
    /// Serializes the input object to a dictionary
    /// </summary>
    IDictionary<string, object?>? ToDictionary();
}
