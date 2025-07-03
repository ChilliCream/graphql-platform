using System.Collections.Immutable;
using HotChocolate.Fusion.Errors;
using HotChocolate.Fusion.Events;
using HotChocolate.Fusion.Events.Contracts;
using HotChocolate.Fusion.Extensions;
using HotChocolate.Fusion.Language;
using HotChocolate.Fusion.Logging.Contracts;
using HotChocolate.Fusion.Results;
using HotChocolate.Fusion.SyntaxWalkers;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using static HotChocolate.Language.Utf8GraphQLParser;
using ArgumentNames = HotChocolate.Fusion.WellKnownArgumentNames;

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
                if (type is MutableObjectTypeDefinition t && t.HasInternalDirective())
                {
                    continue;
                }

                PublishEvent(new TypeEvent(type, schema), context);

                if (type is MutableComplexTypeDefinition complexType)
                {
                    if (complexType.Directives.ContainsName(WellKnownDirectiveNames.Key))
                    {
                        PublishKeyEvents(complexType, schema, context);
                    }

                    foreach (var field in complexType.Fields)
                    {
                        if (field.HasInternalDirective())
                        {
                            continue;
                        }

                        PublishEvent(new OutputFieldEvent(field, type, schema), context);

                        if (field.Directives.ContainsName(WellKnownDirectiveNames.Provides))
                        {
                            PublishProvidesEvents(field, complexType, schema, context);
                        }

                        foreach (var argument in field.Arguments)
                        {
                            PublishEvent(
                                new FieldArgumentEvent(argument, field, type, schema), context);

                            if (argument.Directives.ContainsName(WellKnownDirectiveNames.Is))
                            {
                                PublishIsEvents(
                                    argument,
                                    field,
                                    complexType,
                                    schema,
                                    context);
                            }

                            if (argument.Directives.ContainsName(WellKnownDirectiveNames.Require))
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

    private void PublishKeyEvents(
        MutableComplexTypeDefinition type,
        MutableSchemaDefinition schema,
        CompositionContext context)
    {
        var keyDirectives = type.Directives.AsEnumerable().Where(d => d.Name == WellKnownDirectiveNames.Key);

        foreach (var keyDirective in keyDirectives)
        {
            if (!keyDirective.Arguments.TryGetValue(ArgumentNames.Fields, out var f)
                || f is not StringValueNode fieldsArgument)
            {
                PublishEvent(new KeyFieldsInvalidTypeEvent(keyDirective, type, schema), context);

                continue;
            }

            try
            {
                var selectionSet = Syntax.ParseSelectionSet($"{{{fieldsArgument.Value}}}");

                PublishEvent(new KeyFieldsEvent(selectionSet, keyDirective, type, schema), context);

                var keyFieldNodes =
                    new SelectionSetFieldNodesExtractor().ExtractFieldNodes(selectionSet);

                foreach (var (fieldNode, fieldNamePath) in keyFieldNodes)
                {
                    PublishEvent(
                        new KeyFieldNodeEvent(
                            fieldNode,
                            fieldNamePath,
                            keyDirective,
                            type,
                            schema),
                        context);
                }

                var keyFields =
                    new SelectionSetFieldsExtractor(schema).ExtractFields(selectionSet, type);

                foreach (var (keyField, keyFieldDeclaringType, _) in keyFields)
                {
                    PublishEvent(
                        new KeyFieldEvent(
                            keyField,
                            keyFieldDeclaringType,
                            keyDirective,
                            type,
                            schema),
                        context);
                }
            }
            catch (SyntaxException)
            {
                PublishEvent(new KeyFieldsInvalidSyntaxEvent(keyDirective, type, schema), context);
            }
        }
    }

    private void PublishProvidesEvents(
        MutableOutputFieldDefinition field,
        MutableComplexTypeDefinition type,
        MutableSchemaDefinition schema,
        CompositionContext context)
    {
        var providesDirective = field.Directives.AsEnumerable().First(d => d.Name == WellKnownDirectiveNames.Provides);

        if (!providesDirective.Arguments.TryGetValue(ArgumentNames.Fields, out var f)
            || f is not StringValueNode fieldsArgument)
        {
            PublishEvent(
                new ProvidesFieldsInvalidTypeEvent(providesDirective, field, type, schema),
                context);

            return;
        }

        try
        {
            var selectionSet = Syntax.ParseSelectionSet($"{{{fieldsArgument.Value}}}");

            PublishEvent(
                new ProvidesFieldsEvent(
                    selectionSet,
                    providesDirective,
                    field,
                    type,
                    schema),
                context);

            var providesFieldNodes =
                new SelectionSetFieldNodesExtractor().ExtractFieldNodes(selectionSet);

            foreach (var (fieldNode, fieldNamePath) in providesFieldNodes)
            {
                PublishEvent(
                    new ProvidesFieldNodeEvent(
                        fieldNode,
                        fieldNamePath,
                        providesDirective,
                        field,
                        type,
                        schema),
                    context);
            }

            var providesFields =
                new SelectionSetFieldsExtractor(schema).ExtractFields(
                    selectionSet,
                    field.Type.AsTypeDefinition());

            foreach (var (providesField, providesFieldDeclaringType, _) in providesFields)
            {
                PublishEvent(
                    new ProvidesFieldEvent(
                        providesField,
                        providesFieldDeclaringType,
                        providesDirective,
                        field,
                        type,
                        schema),
                    context);
            }
        }
        catch (SyntaxException)
        {
            PublishEvent(
                new ProvidesFieldsInvalidSyntaxEvent(providesDirective, field, type, schema),
                context);
        }
    }

    private void PublishIsEvents(
        MutableInputFieldDefinition argument,
        MutableOutputFieldDefinition field,
        MutableComplexTypeDefinition type,
        MutableSchemaDefinition schema,
        CompositionContext context)
    {
        var isDirective =
            argument.Directives.AsEnumerable().First(d => d.Name == WellKnownDirectiveNames.Is);

        PublishEvent(new IsDirectiveEvent(isDirective, argument, field, type, schema), context);

        if (!isDirective.Arguments.TryGetValue(ArgumentNames.Field, out var f)
            || f is not StringValueNode fieldArgument)
        {
            PublishEvent(
                new IsFieldInvalidTypeEvent(isDirective, argument, field, type, schema),
                context);

            return;
        }

        try
        {
            new FieldSelectionMapParser(fieldArgument.Value).Parse();
        }
        catch (FieldSelectionMapSyntaxException)
        {
            PublishEvent(
                new IsFieldInvalidSyntaxEvent(
                    isDirective,
                    argument,
                    field,
                    type,
                    schema),
                context);
        }
    }

    private void PublishRequireEvents(
        MutableInputFieldDefinition argument,
        MutableOutputFieldDefinition field,
        MutableComplexTypeDefinition type,
        MutableSchemaDefinition schema,
        CompositionContext context)
    {
        var requireDirective = argument.Directives.AsEnumerable().First(d => d.Name == WellKnownDirectiveNames.Require);

        if (!requireDirective.Arguments.TryGetValue(ArgumentNames.Field, out var f)
            || f is not StringValueNode fieldArgument)
        {
            PublishEvent(
                new RequireFieldInvalidTypeEvent(requireDirective, argument, field, type, schema),
                context);

            return;
        }

        try
        {
            new FieldSelectionMapParser(fieldArgument.Value).Parse();
        }
        catch (FieldSelectionMapSyntaxException)
        {
            PublishEvent(
                new RequireFieldInvalidSyntaxEvent(
                    requireDirective,
                    argument,
                    field,
                    type,
                    schema),
                context);
        }
    }
}
