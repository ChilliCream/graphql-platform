using HotChocolate.Fusion.Events;
using HotChocolate.Fusion.Events.Contracts;
using HotChocolate.Types;
using static HotChocolate.Fusion.Logging.LogEntryHelper;
using static HotChocolate.Fusion.Properties.CompositionResources;

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

        // Scalars.
        foreach (var builtInScalar in FusionBuiltIns.SourceSchemaScalars)
        {
            if (schema.Types.TryGetType(builtInScalar.Name, out var type)
                && type.Kind != TypeKind.Scalar)
            {
                context.Log.Write(TypeDefinitionInvalid(type, schema));
            }
        }

        // Directives.
        foreach (var builtInDirective in FusionBuiltIns.SourceSchemaDirectives)
        {
            if (
                !schema.DirectiveDefinitions.TryGetDirective(
                    builtInDirective.Name,
                    out var directive))
            {
                continue;
            }

            foreach (var expectedArgument in builtInDirective.Arguments)
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
}
