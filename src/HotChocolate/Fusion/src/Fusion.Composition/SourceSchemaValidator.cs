using System.Collections.Immutable;
using HotChocolate.Fusion.Errors;
using HotChocolate.Fusion.Events;
using HotChocolate.Fusion.Events.Contracts;
using HotChocolate.Fusion.Extensions;
using HotChocolate.Fusion.Logging.Contracts;
using HotChocolate.Fusion.Results;
using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion;

internal sealed class SourceSchemaValidator(
    ImmutableSortedSet<MutableSchemaDefinition> schemas,
    ImmutableArray<object> rules,
    ICompositionLog log)
{
    public CompositionResult Validate()
    {
        PublishEvents();

        return log.HasErrors
            ? ErrorHelper.SourceSchemaValidationFailed()
            : CompositionResult.Success();
    }

    private void PublishEvents()
    {
        var context = new CompositionContext(schemas, log);

        foreach (var schema in schemas)
        {
            PublishEvent(new SchemaEvent(schema), context);

            foreach (var type in schema.Types)
            {
                if (type is MutableObjectTypeDefinition { IsInternal: true })
                {
                    continue;
                }

                PublishEvent(new TypeEvent(type, schema), context);

                if (type is MutableComplexTypeDefinition complexType)
                {
                    PublishEvent(new ComplexTypeEvent(complexType, schema), context);

                    foreach (var field in complexType.Fields)
                    {
                        if (field.IsInternal)
                        {
                            continue;
                        }

                        PublishEvent(new OutputFieldEvent(field, type, schema), context);

                        foreach (var argument in field.Arguments)
                        {
                            PublishEvent(
                                new FieldArgumentEvent(argument, field, type, schema), context);
                        }
                    }
                }
            }

            foreach (var directive in schema.DirectiveDefinitions)
            {
                foreach (var argument in directive.Arguments)
                {
                    PublishEvent(new DirectiveArgumentEvent(argument, directive, schema), context);
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
