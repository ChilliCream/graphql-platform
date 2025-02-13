using HotChocolate.Fusion.Events;
using HotChocolate.Fusion.Events.Contracts;
using HotChocolate.Fusion.Extensions;
using static HotChocolate.Fusion.Logging.LogEntryHelper;

namespace HotChocolate.Fusion.PostMergeValidationRules;

/// <summary>
/// <para>
/// In GraphQL, any object type that implements an interface must provide a field definition for
/// every field declared by that interface. If an object type fails to implement a particular field
/// required by one of its interfaces, the composite schema becomes invalid because the resulting
/// schema breaks the contract defined by that interface.
/// </para>
/// <para>
/// This rule checks that object types merged from different sources correctly implement all
/// interface fields. In scenarios where a schema defines an interface field, but the implementing
/// object type in another schema omits that field, an error is raised.
/// </para>
/// </summary>
/// <seealso href="https://graphql.github.io/composite-schemas-spec/draft/#sec-Interface-Field-No-Implementation">
/// Specification
/// </seealso>
internal sealed class InterfaceFieldNoImplementationRule : IEventHandler<ObjectTypeEvent>
{
    public void Handle(ObjectTypeEvent @event, CompositionContext context)
    {
        var (objectType, schema) = @event;

        foreach (var interfaceType in objectType.Implements)
        {
            var accessibleFields =
                interfaceType.Fields.Where(f => !f.HasFusionInaccessibleDirective());

            foreach (var interfaceField in accessibleFields)
            {
                if (!objectType.Fields.ContainsName(interfaceField.Name))
                {
                    context.Log.Write(
                        InterfaceFieldNoImplementation(
                            objectType,
                            interfaceField.Name,
                            interfaceType.Name,
                            schema));
                }
            }
        }
    }
}
