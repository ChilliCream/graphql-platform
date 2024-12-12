using System.Collections.Immutable;
using HotChocolate.Fusion.Collections;
using HotChocolate.Fusion.Errors;
using HotChocolate.Fusion.Events;
using HotChocolate.Fusion.PreMergeValidation.Contracts;
using HotChocolate.Fusion.Results;
using HotChocolate.Skimmed;

namespace HotChocolate.Fusion.PreMergeValidation;

internal sealed class PreMergeValidator(IEnumerable<IPreMergeValidationRule> rules)
{
    private readonly ImmutableArray<IPreMergeValidationRule> _rules = [.. rules];

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
                PublishEvent(new EachTypeEvent(context, type, schema));

                typeGroupByName.Add(type.Name, new TypeInfo(type, schema));

                if (type is ComplexTypeDefinition complexType)
                {
                    foreach (var field in complexType.Fields)
                    {
                        PublishEvent(new EachOutputFieldEvent(context, field, type, schema));

                        foreach (var argument in field.Arguments)
                        {
                            PublishEvent(
                                new EachFieldArgumentEvent(context, argument, field, type, schema));
                        }
                    }
                }
            }

            foreach (var directive in schema.DirectiveDefinitions)
            {
                PublishEvent(new EachDirectiveEvent(context, directive, schema));

                foreach (var argument in directive.Arguments)
                {
                    PublishEvent(
                        new EachDirectiveArgumentEvent(context, argument, directive, schema));
                }
            }
        }

        foreach (var (typeName, typeGroup) in typeGroupByName)
        {
            PublishEvent(new EachTypeGroupEvent(context, typeName, [.. typeGroup]));

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
                    new EachOutputFieldGroupEvent(context, fieldName, [.. fieldGroup], typeName));

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
                            context,
                            argumentName,
                            [.. argumentGroup],
                            fieldName,
                            typeName));
                }
            }
        }
    }

    private void PublishEvent(IEvent @event)
    {
        foreach (var rule in _rules)
        {
            switch (@event)
            {
                case EachTypeEvent e:
                    rule.OnEachType(e);
                    break;

                case EachOutputFieldEvent e:
                    rule.OnEachOutputField(e);
                    break;

                case EachFieldArgumentEvent e:
                    rule.OnEachFieldArgument(e);
                    break;

                case EachDirectiveEvent e:
                    rule.OnEachDirective(e);
                    break;

                case EachDirectiveArgumentEvent e:
                    rule.OnEachDirectiveArgument(e);
                    break;

                case EachTypeGroupEvent e:
                    rule.OnEachTypeGroup(e);
                    break;

                case EachOutputFieldGroupEvent e:
                    rule.OnEachOutputFieldGroup(e);
                    break;

                case EachFieldArgumentGroupEvent e:
                    rule.OnEachFieldArgumentGroup(e);
                    break;
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
