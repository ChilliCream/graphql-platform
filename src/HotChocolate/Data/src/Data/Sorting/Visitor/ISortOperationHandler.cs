using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors.Configurations;

namespace HotChocolate.Data.Sorting;

/// <summary>
/// Represents a handler that can be bound to a <see cref="SortField"/>. The handler is
/// executed during the visitation of an input object.
/// </summary>
public interface ISortOperationHandler
{
    /// <summary>
    /// Tests if this operation handler can handle a field If it can handle the field it
    /// will be attached to the <see cref="SortField"/>
    /// </summary>
    /// <param name="context">The discovery context of the schema</param>
    /// <param name="typeDefinition">The configuration of the declaring type of the field</param>
    /// <param name="valueConfiguration">The configuration of the field</param>
    /// <returns>Returns true if the field can be handled</returns>
    bool CanHandle(
        ITypeCompletionContext context,
        EnumTypeConfiguration typeDefinition,
        SortEnumValueConfiguration valueConfiguration);
}
