using HotChocolate.Configuration;

namespace HotChocolate.Data.Filters;

/// <summary>
/// Represents a handler that can be bound to a <see cref="FilterField"/>
/// </summary>
public interface IFilterFieldHandler
{
    /// <summary>
    /// Tests if this field handler can handle a field If it can handle the field it
    /// will be attached to the <see cref="FilterField"/>
    /// </summary>
    /// <param name="context">The discovery context of the schema</param>
    /// <param name="typeConfiguration">The configuration of the declaring type of the field</param>
    /// <param name="fieldConfiguration">The configuration of the field</param>
    /// <returns>Returns true if the field can be handled</returns>
    bool CanHandle(
        ITypeCompletionContext context,
        IFilterInputTypeConfiguration typeConfiguration,
        IFilterFieldConfiguration fieldConfiguration);
}
