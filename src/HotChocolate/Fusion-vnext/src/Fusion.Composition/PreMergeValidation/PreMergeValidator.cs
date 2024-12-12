using HotChocolate.Fusion.Collections;
using HotChocolate.Fusion.Errors;
using HotChocolate.Fusion.Events;
using HotChocolate.Fusion.PreMergeValidation.Contracts;
using HotChocolate.Fusion.Results;
using HotChocolate.Skimmed;

namespace HotChocolate.Fusion.PreMergeValidation;

internal sealed class PreMergeValidator
{
    private readonly EventAggregator _eventAggregator;

    public PreMergeValidator(IEnumerable<object> rules)
    {
        _eventAggregator = new EventAggregator();

        SubscribeRules(rules);
    }

    public CompositionResult Validate(CompositionContext context)
    {
        PublishEvents(context);

        return context.Log.HasErrors
            ? ErrorHelper.PreMergeValidationFailed()
            : CompositionResult.Success();
    }

    private void SubscribeRules(IEnumerable<object> rules)
    {
        foreach (var rule in rules)
        {
            if (rule is IEachTypeEventHandler eachTypeEventHandler)
            {
                _eventAggregator.Subscribe<EachTypeEvent>(
                    eachTypeEventHandler.OnEachType);
            }

            if (rule is IEachOutputFieldEventHandler eachOutputFieldEventHandler)
            {
                _eventAggregator.Subscribe<EachOutputFieldEvent>(
                    eachOutputFieldEventHandler.OnEachOutputField);
            }

            if (rule is IEachFieldArgumentEventHandler eachFieldArgumentEventHandler)
            {
                _eventAggregator.Subscribe<EachFieldArgumentEvent>(
                    eachFieldArgumentEventHandler.OnEachFieldArgument);
            }

            if (rule is IEachDirectiveEventHandler eachDirectiveEventHandler)
            {
                _eventAggregator.Subscribe<EachDirectiveEvent>(
                    eachDirectiveEventHandler.OnEachDirective);
            }

            if (rule is IEachDirectiveArgumentEventHandler eachDirectiveArgumentEventHandler)
            {
                _eventAggregator.Subscribe<EachDirectiveArgumentEvent>(
                    eachDirectiveArgumentEventHandler.OnEachDirectiveArgument);
            }

            if (rule is IEachOutputFieldNameEventHandler eachOutputFieldNameEventHandler)
            {
                _eventAggregator.Subscribe<EachOutputFieldNameEvent>(
                    eachOutputFieldNameEventHandler.OnEachOutputFieldName);
            }
        }
    }

    private void PublishEvents(CompositionContext context)
    {
        MultiValueDictionary<string, TypeInfo> typeInfoByName = [];

        foreach (var schema in context.SchemaDefinitions)
        {
            foreach (var type in schema.Types)
            {
                _eventAggregator.Publish(new EachTypeEvent(context, type, schema));

                typeInfoByName.Add(type.Name, new TypeInfo(type, schema));

                if (type is ComplexTypeDefinition complexType)
                {
                    foreach (var field in complexType.Fields)
                    {
                        _eventAggregator.Publish(
                            new EachOutputFieldEvent(context, field, type, schema));

                        foreach (var argument in field.Arguments)
                        {
                            _eventAggregator.Publish(
                                new EachFieldArgumentEvent(context, argument, field, type, schema));
                        }
                    }
                }
            }

            foreach (var directive in schema.DirectiveDefinitions)
            {
                _eventAggregator.Publish(
                    new EachDirectiveEvent(context, directive, schema));

                foreach (var argument in directive.Arguments)
                {
                    _eventAggregator.Publish(
                        new EachDirectiveArgumentEvent(context, argument, directive, schema));
                }
            }
        }

        foreach (var (typeName, typeInfo) in typeInfoByName)
        {
            _eventAggregator.Publish(new EachTypeNameEvent(context, typeName, [.. typeInfo]));

            MultiValueDictionary<string, OutputFieldInfo> fieldInfoByName = [];

            foreach (var (type, schema) in typeInfo)
            {
                if (type is ComplexTypeDefinition complexType)
                {
                    foreach (var field in complexType.Fields)
                    {
                        fieldInfoByName.Add(field.Name, new OutputFieldInfo(field, type, schema));
                    }
                }
            }

            foreach (var (fieldName, fieldInfo) in fieldInfoByName)
            {
                _eventAggregator.Publish(
                    new EachOutputFieldNameEvent(context, fieldName, [.. fieldInfo], typeName));

                MultiValueDictionary<string, FieldArgumentInfo> argumentInfoByName = [];

                foreach (var (field, type, schema) in fieldInfo)
                {
                    foreach (var argument in field.Arguments)
                    {
                        argumentInfoByName.Add(
                            argument.Name,
                            new FieldArgumentInfo(argument, field, type, schema));
                    }
                }

                foreach (var (argumentName, argumentInfo) in argumentInfoByName)
                {
                    _eventAggregator.Publish(
                        new EachFieldArgumentNameEvent(
                            context,
                            argumentName,
                            [.. argumentInfo],
                            fieldName,
                            typeName));
                }
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
