using System.Collections.Immutable;
using HotChocolate.Fusion.Collections;
using HotChocolate.Fusion.Errors;
using HotChocolate.Fusion.Events;
using HotChocolate.Fusion.Results;
using HotChocolate.Skimmed;

namespace HotChocolate.Fusion.PreMergeValidation;

internal sealed class PreMergeValidator(IEnumerable<object> rules)
{
    private readonly ImmutableArray<object> _rules = [.. rules];

    public CompositionResult Validate(CompositionContext context)
    {
        PublishEvents(context);

        return context.Log.HasErrors
            ? ErrorHelper.PreMergeValidationFailed()
            : CompositionResult.Success();
    }

    private void PublishEvents(CompositionContext context)
    {
        MultiValueDictionary<string, TypeInfo> typeGroupByName = [];

        foreach (var schema in context.SchemaDefinitions)
        {
            foreach (var type in schema.Types)
            {
                PublishEvent(new EachTypeEvent(type, schema), context);

                typeGroupByName.Add(type.Name, new TypeInfo(type, schema));

                if (type is ComplexTypeDefinition complexType)
                {
                    foreach (var field in complexType.Fields)
                    {
                        PublishEvent(new EachOutputFieldEvent(field, type, schema), context);

                        foreach (var argument in field.Arguments)
                        {
                            PublishEvent(
                                new EachFieldArgumentEvent(argument, field, type, schema), context);
                        }
                    }
                }
            }

            foreach (var directive in schema.DirectiveDefinitions)
            {
                PublishEvent(new EachDirectiveEvent(directive, schema), context);

                foreach (var argument in directive.Arguments)
                {
                    PublishEvent(
                        new EachDirectiveArgumentEvent(argument, directive, schema), context);
                }
            }
        }

        foreach (var (typeName, typeGroup) in typeGroupByName)
        {
            PublishEvent(new EachTypeGroupEvent(typeName, [.. typeGroup]), context);

            MultiValueDictionary<string, OutputFieldInfo> fieldGroupByName = [];

            foreach (var (type, schema) in typeGroup)
            {
                if (type is ComplexTypeDefinition complexType)
                {
                    foreach (var field in complexType.Fields)
                    {
                        fieldGroupByName.Add(field.Name, new OutputFieldInfo(field, type, schema));
                    }
                }
            }

            foreach (var (fieldName, fieldGroup) in fieldGroupByName)
            {
                PublishEvent(
                    new EachOutputFieldGroupEvent(fieldName, [.. fieldGroup], typeName), context);

                MultiValueDictionary<string, FieldArgumentInfo> argumentGroupByName = [];

                foreach (var (field, type, schema) in fieldGroup)
                {
                    foreach (var argument in field.Arguments)
                    {
                        argumentGroupByName.Add(
                            argument.Name,
                            new FieldArgumentInfo(argument, field, type, schema));
                    }
                }

                foreach (var (argumentName, argumentGroup) in argumentGroupByName)
                {
                    PublishEvent(
                        new EachFieldArgumentGroupEvent(
                            argumentName,
                            [.. argumentGroup],
                            fieldName,
                            typeName),
                        context);
                }
            }
        }
    }

    private void PublishEvent<TEvent>(TEvent @event, CompositionContext context)
        where TEvent : IEvent
    {
        foreach (var rule in _rules)
        {
            if (rule is IEventHandler<TEvent> handler)
            {
                handler.Handle(@event, context);
            }
        }
    }
}

internal record TypeInfo(
    INamedTypeDefinition Type,
    SchemaDefinition Schema);

internal record OutputFieldInfo(
    OutputFieldDefinition Field,
    INamedTypeDefinition Type,
    SchemaDefinition Schema);

internal record FieldArgumentInfo(
    InputFieldDefinition Argument,
    OutputFieldDefinition Field,
    INamedTypeDefinition Type,
    SchemaDefinition Schema);
