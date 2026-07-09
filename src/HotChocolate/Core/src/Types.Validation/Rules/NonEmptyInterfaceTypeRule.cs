using HotChocolate.Events;
using HotChocolate.Events.Contracts;
using static HotChocolate.Logging.LogEntryHelper;

namespace HotChocolate.Rules;

/// <summary>
/// An Interface type must define one or more fields.
/// </summary>
/// <seealso href="https://spec.graphql.org/draft/#sec-Interfaces.Type-Validation">
/// Specification
/// </seealso>
public sealed class NonEmptyInterfaceTypeRule : IValidationEventHandler<InterfaceTypeEvent>
{
    /// <summary>
    /// Checks that the Interface type defines one or more fields.
    /// </summary>
    public void Handle(InterfaceTypeEvent @event, ValidationContext context)
    {
        var interfaceType = @event.InterfaceType;

        if (!interfaceType.Fields.Any())
        {
            context.Log.Write(EmptyInterfaceType(interfaceType));
        }
    }
}
