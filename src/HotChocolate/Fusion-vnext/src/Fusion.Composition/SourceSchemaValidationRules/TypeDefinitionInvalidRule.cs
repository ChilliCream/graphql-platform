using System.Collections.Frozen;
using HotChocolate.Fusion.Definitions;
using HotChocolate.Fusion.Events;
using HotChocolate.Fusion.Events.Contracts;
using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using static HotChocolate.Fusion.Logging.LogEntryHelper;
using static HotChocolate.Fusion.Properties.CompositionResources;
using static HotChocolate.Fusion.WellKnownTypeNames;

namespace HotChocolate.Fusion.SourceSchemaValidationRules;

/// <summary>
/// Certain types (and directives) are reserved in composite schema specification for specific
/// purposes and must adhere to the specification’s definitions. For example,
/// <c>FieldSelectionMap</c> is a built-in scalar that represents a selection of fields as a string.
/// Redefining these built-in types with a different kind (e.g., an input object, enum, union, or
/// object type) is disallowed and makes the composition invalid.
/// </summary>
/// <seealso href="https://graphql.github.io/composite-schemas-spec/draft/#sec-Type-Definition-Invalid">
/// Specification
/// </seealso>
internal sealed class TypeDefinitionInvalidRule : IEventHandler<SchemaEvent>
{
    public void Handle(SchemaEvent @event, CompositionContext context)
    {
        var schema = @event.Schema;

        // Types.
        if (schema.Types.TryGetType(FieldSelectionMap, out var fieldSelectionMapType)
            && fieldSelectionMapType.Kind != TypeKind.Scalar)
        {
            context.Log.Write(TypeDefinitionInvalid(fieldSelectionMapType, schema));
        }

        if (schema.Types.TryGetType(FieldSelectionSet, out var fieldSelectionSetType)
            && fieldSelectionSetType.Kind != TypeKind.Scalar)
        {
            context.Log.Write(TypeDefinitionInvalid(fieldSelectionSetType, schema));
        }

        // Directives.
        foreach (var (name, definition) in _builtInDirectives)
        {
            if (!schema.DirectiveDefinitions.TryGetDirective(name, out var directive))
            {
                continue;
            }

            foreach (var expectedArgument in definition.Arguments)
            {
                var argumentName = expectedArgument.Name;

                if (!directive.Arguments.TryGetField(argumentName, out var argument))
                {
                    context.Log.Write(
                        TypeDefinitionInvalid(
                            directive,
                            schema,
                            details: string.Format(
                                TypeDefinitionInvalidRule_ArgumentMissing,
                                argumentName)));

                    continue;
                }

                var expectedType = expectedArgument.Type;

                if (!argument.Type.Equals(expectedType, TypeComparison.Structural))
                {
                    context.Log.Write(
                        TypeDefinitionInvalid(
                            directive,
                            schema,
                            details: string.Format(
                                TypeDefinitionInvalidRule_ArgumentTypeDifferent,
                                argumentName)));
                }
            }
        }
    }

    private readonly FrozenDictionary<string, MutableDirectiveDefinition> _builtInDirectives =
        CreateBuiltInDirectiveDefinitions();

    private static FrozenDictionary<string, MutableDirectiveDefinition>
        CreateBuiltInDirectiveDefinitions()
    {
        var fieldSelectionMapType = MutableScalarTypeDefinition.Create(FieldSelectionMap);
        var fieldSelectionSetType = MutableScalarTypeDefinition.Create(FieldSelectionSet);
        var stringType = BuiltIns.String.Create();

        return new Dictionary<string, MutableDirectiveDefinition>()
        {
            { "external", new ExternalMutableDirectiveDefinition() },
            { "inaccessible", new InaccessibleMutableDirectiveDefinition() },
            { "internal", new InternalMutableDirectiveDefinition() },
            { "is", new IsMutableDirectiveDefinition(fieldSelectionMapType) },
            { "key", new KeyMutableDirectiveDefinition(fieldSelectionSetType) },
            { "lookup", new LookupMutableDirectiveDefinition() },
            { "override", new OverrideMutableDirectiveDefinition(stringType) },
            { "provides", new ProvidesMutableDirectiveDefinition(fieldSelectionSetType) },
            { "require", new RequireMutableDirectiveDefinition(fieldSelectionMapType) },
            { "schemaName", new SchemaNameMutableDirectiveDefinition(stringType) },
            { "shareable", new ShareableMutableDirectiveDefinition() }
        }.ToFrozenDictionary();
    }
}
