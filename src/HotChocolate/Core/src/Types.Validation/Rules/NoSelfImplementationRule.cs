using HotChocolate.Events;
using HotChocolate.Events.Contracts;
using static HotChocolate.Logging.LogEntryHelper;

namespace HotChocolate.Rules;

/// <summary>
/// An interface type may declare that it implements one or more unique interfaces, but may not
/// implement itself.
/// </summary>
/// <seealso href="https://spec.graphql.org/draft/#sec-Interfaces.Type-Validation">
/// Specification
/// </seealso>
public sealed class NoSelfImplementationRule : IValidationEventHandler<InterfaceTypeEvent>
{
    /// <summary>
    /// Checks that the Interface type does not implement itself.
    /// </summary>
    public void Handle(InterfaceTypeEvent @event, ValidationContext context)
    {
        var interfaceType = @event.InterfaceType;

        foreach (var implementedType in interfaceType.Implements)
        {
            if (implementedType == interfaceType)
            {
                context.Log.Write(SelfImplementation(interfaceType));
            }
        }
    }
}
