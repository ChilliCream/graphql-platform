using HotChocolate.Fusion.Events;
using HotChocolate.Fusion.Events.Contracts;
using HotChocolate.Fusion.Extensions;
using HotChocolate.Types.Mutable;
using static HotChocolate.Fusion.Logging.LogEntryHelper;

namespace HotChocolate.Fusion.PostMergeValidationRules;

/// <summary>
/// This rule ensures that inaccessible fields (<c>@inaccessible</c>) on an object or interface type
/// are not exposed through an interface. A composite type that implements an interface must provide
/// public access to each field defined by the interface. If a field on an object type is marked as
/// <c>@inaccessible</c> but implements an interface field that is visible in the composed schema,
/// this creates a contradiction: the interface contract requires that field to be accessible, yet
/// the implementation hides it.
/// </summary>
/// <seealso href="https://graphql.github.io/composite-schemas-spec/draft/#sec-Implemented-by-Inaccessible">
/// Specification
/// </seealso>
internal sealed class ImplementedByInaccessibleRule
    : IEventHandler<ObjectTypeEvent>
    , IEventHandler<InterfaceTypeEvent>
{
    public void Handle(ObjectTypeEvent @event, CompositionContext context)
    {
        var (objectType, schema) = @event;

        Handle(objectType, schema, context);
    }

    public void Handle(InterfaceTypeEvent @event, CompositionContext context)
    {
        var (interfaceType, schema) = @event;

        Handle(interfaceType, schema, context);
    }

    private static void Handle(
        MutableComplexTypeDefinition type,
        MutableSchemaDefinition schema,
        CompositionContext context)
    {
        var accessibleImplementedInterfaces =
            type.Implements
                .AsEnumerable()
                .Where(i => !i.HasFusionInaccessibleDirective());

        foreach (var interfaceType in accessibleImplementedInterfaces)
        {
            var accessibleInterfaceFields =
                interfaceType.Fields.AsEnumerable().Where(f => !f.HasFusionInaccessibleDirective());

            foreach (var interfaceField in accessibleInterfaceFields)
            {
                var field = type.Fields[interfaceField.Name];

                if (field.HasFusionInaccessibleDirective()
                    || type.HasFusionInaccessibleDirective())
                {
                    context.Log.Write(
                        ImplementedByInaccessible(
                            field,
                            type.Name,
                            interfaceField.Name,
                            interfaceType.Name,
                            schema));
                }
            }
        }
    }
}
