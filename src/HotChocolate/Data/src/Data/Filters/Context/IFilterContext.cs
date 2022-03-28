using System.Collections.Generic;

namespace HotChocolate.Data.Filters;

/// <summary>
/// Encapuslates all filter specific information
/// </summary>
public interface IFilterContext : IFilterValue
{
    /// <summary>
    /// Enables the filter exceution if <paramref name="skip"/> is <c>true</c>.
    /// You want to enable the execution of the filtering when you do not handle the execution
    /// manually
    /// </summary>
    /// <param name="enable">If true, filtering is applied on the result of the resolver</param>
    void EnableFilterExecution(bool enable = true);

    /// <summary>
    /// Serializes the input object to a dictionary
    /// </summary>
    IDictionary<string, object?>? ToDictionary();
}
