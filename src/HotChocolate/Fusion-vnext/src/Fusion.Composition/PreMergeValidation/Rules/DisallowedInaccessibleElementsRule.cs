using HotChocolate.Fusion.Errors;
using HotChocolate.Fusion.PreMergeValidation.Contracts;
using HotChocolate.Fusion.Results;
using HotChocolate.Skimmed;
using static HotChocolate.Fusion.Logging.LogEntryHelper;

namespace HotChocolate.Fusion.PreMergeValidation.Rules;

/// <summary>
/// This rule ensures that certain essential elements of a GraphQL schema, particularly built-in
/// scalars, directive arguments, and introspection types, cannot be marked as @inaccessible. These
/// types are fundamental to GraphQL. Making these elements inaccessible would break core GraphQL
/// functionality.
/// </summary>
/// <seealso href="https://graphql.github.io/composite-schemas-spec/draft/#sec-Disallowed-Inaccessible-Elements">
/// Specification
/// </seealso>
internal sealed class DisallowedInaccessibleElementsRule : IPreMergeValidationRule
{
    public CompositionResult Run(PreMergeValidationContext context)
    {
        var loggingSession = context.Log.CreateSession();

        foreach (var schema in context.SchemaDefinitions)
        {
            foreach (var type in schema.Types)
            {
                if (type is ScalarTypeDefinition { IsSpecScalar: true } scalar
                    && !ValidationHelper.IsAccessible(type))
                {
                    loggingSession.Write(DisallowedInaccessibleScalar(scalar, schema));
                }

                if (type.IsIntrospectionType)
                {
                    if (!ValidationHelper.IsAccessible(type))
                    {
                        loggingSession.Write(DisallowedInaccessibleIntrospectionType(type, schema));
                    }

                    if (type is ComplexTypeDefinition complexType)
                    {
                        foreach (var field in complexType.Fields)
                        {
                            if (!ValidationHelper.IsAccessible(field))
                            {
                                loggingSession.Write(
                                    DisallowedInaccessibleIntrospectionField(
                                        field,
                                        type.Name,
                                        schema));
                            }

                            foreach (var argument in field.Arguments)
                            {
                                if (!ValidationHelper.IsAccessible(argument))
                                {
                                    loggingSession.Write(
                                        DisallowedInaccessibleIntrospectionArgument(
                                            argument,
                                            field.Name,
                                            type.Name,
                                            schema));
                                }
                            }
                        }
                    }
                }
            }

            foreach (var directive in schema.DirectiveDefinitions)
            {
                if (BuiltIns.IsBuiltInDirective(directive.Name))
                {
                    foreach (var argument in directive.Arguments)
                    {
                        if (!ValidationHelper.IsAccessible(argument))
                        {
                            loggingSession.Write(
                                DisallowedInaccessibleDirectiveArgument(
                                    argument,
                                    directive.Name,
                                    schema));
                        }
                    }
                }
            }
        }

        return loggingSession.ErrorCount == 0
            ? CompositionResult.Success()
            : ErrorHelper.PreMergeValidationRuleFailed(this);
    }
}
