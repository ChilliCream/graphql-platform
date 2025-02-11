using System.Collections.Immutable;
using HotChocolate.Fusion.Collections;
using HotChocolate.Fusion.Errors;
using HotChocolate.Fusion.Events;
using HotChocolate.Fusion.Events.Contracts;
using HotChocolate.Fusion.Info;
using HotChocolate.Fusion.Logging.Contracts;
using HotChocolate.Fusion.Results;
using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion;

internal sealed class PreMergeValidator(
    ImmutableSortedSet<SchemaDefinition> schemas,
    ImmutableArray<object> rules,
    ICompositionLog log)
{
    public CompositionResult Validate()
    {
        PublishEvents();

        return log.HasErrors
            ? ErrorHelper.PreMergeValidationFailed()
            : CompositionResult.Success();
    }

    private void PublishEvents()
    {
        var context = new CompositionContext(schemas, log);
        MultiValueDictionary<string, TypeInfo> typeGroupByName = [];

        foreach (var schema in context.SchemaDefinitions)
        {
            foreach (var type in schema.Types)
            {
                typeGroupByName.Add(type.Name, new TypeInfo(type, schema));
            }
        }

        foreach (var (typeName, typeGroup) in typeGroupByName)
        {
            PublishEvent(new TypeGroupEvent(typeName, [.. typeGroup]), context);

            MultiValueDictionary<string, InputTypeInfo> inputTypeGroupByName = [];
            MultiValueDictionary<string, InputFieldInfo> inputFieldGroupByName = [];
            MultiValueDictionary<string, OutputFieldInfo> outputFieldGroupByName = [];
            MultiValueDictionary<string, EnumTypeInfo> enumTypeGroupByName = [];

            foreach (var (type, schema) in typeGroup)
            {
                switch (type)
                {
                    case InputObjectTypeDefinition inputType:
                        inputTypeGroupByName.Add(
                            inputType.Name,
                            new InputTypeInfo(inputType, schema));

                        foreach (var field in inputType.Fields)
                        {
                            inputFieldGroupByName.Add(
                                field.Name,
                                new InputFieldInfo(field, inputType, schema));
                        }

                        break;

                    case MutableComplexTypeDefinition complexType:
                        foreach (var field in complexType.Fields)
                        {
                            outputFieldGroupByName.Add(
                                field.Name,
                                new OutputFieldInfo(field, complexType, schema));
                        }

                        break;

                    case MutableEnumTypeDefinition enumType:
                        enumTypeGroupByName.Add(enumType.Name, new EnumTypeInfo(enumType, schema));
                        break;
                }
            }

            foreach (var (inputTypeName, inputTypeGroup) in inputTypeGroupByName)
            {
                PublishEvent(new InputTypeGroupEvent(inputTypeName, [.. inputTypeGroup]), context);
            }

            foreach (var (fieldName, fieldGroup) in inputFieldGroupByName)
            {
                PublishEvent(
                    new InputFieldGroupEvent(fieldName, [.. fieldGroup], typeName), context);
            }

            foreach (var (fieldName, fieldGroup) in outputFieldGroupByName)
            {
                PublishEvent(
                    new OutputFieldGroupEvent(fieldName, [.. fieldGroup], typeName), context);

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
                        new FieldArgumentGroupEvent(
                            argumentName,
                            [.. argumentGroup],
                            fieldName,
                            typeName),
                        context);
                }
            }

            foreach (var (enumName, enumGroup) in enumTypeGroupByName)
            {
                PublishEvent(new EnumTypeGroupEvent(enumName, [.. enumGroup]), context);
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
