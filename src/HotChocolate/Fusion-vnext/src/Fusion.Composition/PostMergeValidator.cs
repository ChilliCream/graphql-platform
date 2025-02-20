using System.Collections.Immutable;
using HotChocolate.Fusion.Errors;
using HotChocolate.Fusion.Events;
using HotChocolate.Fusion.Events.Contracts;
using HotChocolate.Fusion.Logging.Contracts;
using HotChocolate.Fusion.Results;
using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion;

internal sealed class PostMergeValidator(
    MutableSchemaDefinition mergedSchema,
    ImmutableArray<object> rules,
    ImmutableSortedSet<MutableSchemaDefinition> sourceSchemas,
    ICompositionLog log)
{
    public CompositionResult Validate()
    {
        PublishEvents();

        return log.HasErrors
            ? ErrorHelper.PostMergeValidationFailed()
            : CompositionResult.Success();
    }

    private void PublishEvents()
    {
        var context = new CompositionContext(sourceSchemas, log);

        PublishEvent(new SchemaEvent(mergedSchema), context);

        foreach (var type in mergedSchema.Types)
        {
            switch (type)
            {
                case MutableEnumTypeDefinition enumType:
                    PublishEvent(new EnumTypeEvent(enumType, mergedSchema), context);
                    break;

                case MutableInputObjectTypeDefinition inputType:
                    PublishEvent(new InputTypeEvent(inputType, mergedSchema), context);

                    foreach (var field in inputType.Fields)
                    {
                        PublishEvent(new InputFieldEvent(field, inputType, mergedSchema), context);
                    }

                    break;

                case MutableInterfaceTypeDefinition interfaceType:
                    PublishEvent(new InterfaceTypeEvent(interfaceType, mergedSchema), context);
                    break;

                case MutableObjectTypeDefinition objectType:
                    PublishEvent(new ObjectTypeEvent(objectType, mergedSchema), context);
                    break;

                case MutableUnionTypeDefinition unionType:
                    PublishEvent(new UnionTypeEvent(unionType, mergedSchema), context);
                    break;
            }

            if (type is MutableComplexTypeDefinition complexType)
            {
                foreach (var field in complexType.Fields)
                {
                    PublishEvent(new OutputFieldEvent(field, complexType, mergedSchema), context);

                    foreach (var argument in field.Arguments)
                    {
                        PublishEvent(
                            new FieldArgumentEvent(argument, field, complexType, mergedSchema),
                            context);
                    }
                }
            }
        }
    }

    private void PublishEvent<TEvent>(TEvent @event, CompositionContext context)
        where TEvent : IEvent
    {
        foreach (var rule in rules)
        {
            if (rule is IEventHandler<TEvent> handler)
            {
                handler.Handle(@event, context);
            }
        }
    }
}
