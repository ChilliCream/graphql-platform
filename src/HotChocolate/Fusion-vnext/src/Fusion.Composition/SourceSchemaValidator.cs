using System.Collections.Immutable;
using HotChocolate.Fusion.Errors;
using HotChocolate.Fusion.Events;
using HotChocolate.Fusion.Logging.Contracts;
using HotChocolate.Fusion.Results;
using HotChocolate.Fusion.SourceSchemaValidation;
using HotChocolate.Language;
using HotChocolate.Skimmed;
using static HotChocolate.Language.Utf8GraphQLParser;
using ArgumentNames = HotChocolate.Fusion.WellKnownArgumentNames;
using DirectiveNames = HotChocolate.Fusion.WellKnownDirectiveNames;

namespace HotChocolate.Fusion;

internal sealed class SourceSchemaValidator(
    ImmutableSortedSet<SchemaDefinition> schemas,
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
                PublishEvent(new TypeEvent(type, schema), context);

                if (type is ComplexTypeDefinition complexType)
                {
                    if (complexType.Directives.ContainsName(DirectiveNames.Key))
                    {
                        PublishEntityEvents(complexType, schema, context);
                    }

                    foreach (var field in complexType.Fields)
                    {
                        PublishEvent(new OutputFieldEvent(field, type, schema), context);

                        if (field.Directives.ContainsName(DirectiveNames.Provides))
                        {
                            PublishProvidesEvents(field, complexType, schema, context);
                        }

                        foreach (var argument in field.Arguments)
                        {
                            PublishEvent(
                                new FieldArgumentEvent(argument, field, type, schema), context);

                            if (argument.Directives.ContainsName(DirectiveNames.Require))
                            {
                                PublishRequireEvents(
                                    argument,
                                    field,
                                    complexType,
                                    schema,
                                    context);
                            }
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

    private void PublishEntityEvents(
        ComplexTypeDefinition entityType,
        SchemaDefinition schema,
        CompositionContext context)
    {
        var keyDirectives = entityType.Directives.Where(d => d.Name == DirectiveNames.Key);

        foreach (var keyDirective in keyDirectives)
        {
            if (!keyDirective.Arguments.TryGetValue(ArgumentNames.Fields, out var f)
                || f is not StringValueNode fields)
            {
                PublishEvent(
                    new KeyFieldsInvalidTypeEvent(keyDirective, entityType, schema),
                    context);

                continue;
            }

            try
            {
                var selectionSet = Syntax.ParseSelectionSet($"{{{fields.Value}}}");

                PublishKeyFieldEvents(
                    selectionSet,
                    entityType,
                    keyDirective,
                    [],
                    entityType,
                    schema,
                    context);
            }
            catch (SyntaxException)
            {
                PublishEvent(
                    new KeyFieldsInvalidSyntaxEvent(keyDirective, entityType, schema),
                    context);
            }
        }
    }

    private void PublishKeyFieldEvents(
        SelectionSetNode selectionSet,
        ComplexTypeDefinition entityType,
        Directive keyDirective,
        List<string> fieldNamePath,
        ComplexTypeDefinition? parentType,
        SchemaDefinition schema,
        CompositionContext context)
    {
        ComplexTypeDefinition? nextParentType = null;

        foreach (var selection in selectionSet.Selections)
        {
            if (selection is FieldNode fieldNode)
            {
                fieldNamePath.Add(fieldNode.Name.Value);

                PublishEvent(
                    new KeyFieldNodeEvent(
                        fieldNode,
                        [.. fieldNamePath],
                        keyDirective,
                        entityType,
                        schema),
                    context);

                if (parentType is not null)
                {
                    if (parentType.Fields.TryGetField(fieldNode.Name.Value, out var field))
                    {
                        PublishEvent(
                            new KeyFieldEvent(
                                keyDirective,
                                entityType,
                                field,
                                parentType,
                                schema),
                            context);

                        if (field.Type.NullableType() is ComplexTypeDefinition fieldType)
                        {
                            nextParentType = fieldType;
                        }
                    }
                    else
                    {
                        PublishEvent(
                            new KeyFieldsInvalidReferenceEvent(
                                fieldNode,
                                parentType,
                                keyDirective,
                                entityType,
                                schema),
                            context);

                        nextParentType = null;
                    }
                }

                if (fieldNode.SelectionSet is not null)
                {
                    PublishKeyFieldEvents(
                        fieldNode.SelectionSet,
                        entityType,
                        keyDirective,
                        fieldNamePath,
                        nextParentType,
                        schema,
                        context);
                }

                fieldNamePath = [];
            }
        }
    }

    private void PublishProvidesEvents(
        OutputFieldDefinition field,
        ComplexTypeDefinition type,
        SchemaDefinition schema,
        CompositionContext context)
    {
        var providesDirective = field.Directives.First(d => d.Name == DirectiveNames.Provides);

        if (!providesDirective.Arguments.TryGetValue(ArgumentNames.Fields, out var f)
            || f is not StringValueNode fields)
        {
            PublishEvent(
                new ProvidesFieldsInvalidTypeEvent(providesDirective, field, type, schema),
                context);

            return;
        }

        try
        {
            var selectionSet = Syntax.ParseSelectionSet($"{{{fields.Value}}}");

            PublishProvidesFieldEvents(
                selectionSet,
                field,
                type,
                providesDirective,
                [],
                field.Type,
                schema,
                context);
        }
        catch (SyntaxException)
        {
            PublishEvent(
                new ProvidesFieldsInvalidSyntaxEvent(providesDirective, field, type, schema),
                context);
        }
    }

    private void PublishProvidesFieldEvents(
        SelectionSetNode selectionSet,
        OutputFieldDefinition field,
        ComplexTypeDefinition type,
        Directive providesDirective,
        List<string> fieldNamePath,
        ITypeDefinition? parentType,
        SchemaDefinition schema,
        CompositionContext context)
    {
        ComplexTypeDefinition? nextParentType = null;

        foreach (var selection in selectionSet.Selections)
        {
            if (selection is FieldNode fieldNode)
            {
                fieldNamePath.Add(fieldNode.Name.Value);

                PublishEvent(
                    new ProvidesFieldNodeEvent(
                        fieldNode,
                        [.. fieldNamePath],
                        providesDirective,
                        field,
                        type,
                        schema),
                    context);

                if (parentType?.NullableType() is ComplexTypeDefinition providedType)
                {
                    if (providedType.Fields.TryGetField(
                            fieldNode.Name.Value,
                            out var providedField))
                    {
                        PublishEvent(
                            new ProvidesFieldEvent(
                                providedField,
                                providedType,
                                providesDirective,
                                field,
                                type,
                                schema),
                            context);

                        if (providedField.Type.NullableType() is ComplexTypeDefinition fieldType)
                        {
                            nextParentType = fieldType;
                        }
                    }
                    else
                    {
                        nextParentType = null;
                    }
                }

                if (fieldNode.SelectionSet is not null)
                {
                    PublishProvidesFieldEvents(
                        fieldNode.SelectionSet,
                        field,
                        type,
                        providesDirective,
                        fieldNamePath,
                        nextParentType,
                        schema,
                        context);
                }

                fieldNamePath = [];
            }
        }
    }

    private void PublishRequireEvents(
        InputFieldDefinition argument,
        OutputFieldDefinition field,
        ComplexTypeDefinition type,
        SchemaDefinition schema,
        CompositionContext context)
    {
        var requireDirective = argument.Directives.First(d => d.Name == DirectiveNames.Require);

        if (!requireDirective.Arguments.TryGetValue(ArgumentNames.Fields, out var f)
            || f is not StringValueNode fields)
        {
            PublishEvent(
                new RequireFieldsInvalidTypeEvent(requireDirective, argument, field, type, schema),
                context);

            return;
        }

        try
        {
            var selectionSet = Syntax.ParseSelectionSet($"{{{fields.Value}}}");

            PublishRequireFieldEvents(
                selectionSet,
                argument,
                field,
                type,
                requireDirective,
                [],
                schema,
                context);
        }
        catch (SyntaxException)
        {
            PublishEvent(
                new RequireFieldsInvalidSyntaxEvent(
                    requireDirective,
                    argument,
                    field,
                    type,
                    schema),
                context);
        }
    }

    private void PublishRequireFieldEvents(
        SelectionSetNode selectionSet,
        InputFieldDefinition argument,
        OutputFieldDefinition field,
        ComplexTypeDefinition type,
        Directive requireDirective,
        List<string> fieldNamePath,
        SchemaDefinition schema,
        CompositionContext context)
    {
        foreach (var selection in selectionSet.Selections)
        {
            if (selection is FieldNode fieldNode)
            {
                fieldNamePath.Add(fieldNode.Name.Value);

                PublishEvent(
                    new RequireFieldNodeEvent(
                        fieldNode,
                        [.. fieldNamePath],
                        requireDirective,
                        argument,
                        field,
                        type,
                        schema),
                    context);

                if (fieldNode.SelectionSet is not null)
                {
                    PublishRequireFieldEvents(
                        fieldNode.SelectionSet,
                        argument,
                        field,
                        type,
                        requireDirective,
                        fieldNamePath,
                        schema,
                        context);
                }

                fieldNamePath = [];
            }
        }
    }
}
