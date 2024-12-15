using HotChocolate.Fusion.Events;
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
internal sealed class ExternalArgumentsDefaultMismatchRule
    : IEventHandler<TypeEvent>
    , IEventHandler<OutputFieldEvent>
    , IEventHandler<FieldArgumentEvent>
    , IEventHandler<DirectiveArgumentEvent>
{
    public void Handle(TypeEvent @event, CompositionContext context)
    {
        var (type, schema) = @event;

        // Built-in scalar types must be accessible.
        if (type is ScalarTypeDefinition { IsSpecScalar: true } scalar
            && !ValidationHelper.IsAccessible(scalar))
        {
            context.Log.Write(DisallowedInaccessibleBuiltInScalar(scalar, schema));
        }

        // Introspection types must be accessible.
        if (type.IsIntrospectionType && !ValidationHelper.IsAccessible(type))
        {
            context.Log.Write(DisallowedInaccessibleIntrospectionType(type, schema));
        }
    }

    public void Handle(OutputFieldEvent @event, CompositionContext context)
    {
        var (field, type, schema) = @event;

        // Introspection fields must be accessible.
        if (type.IsIntrospectionType && !ValidationHelper.IsAccessible(field))
        {
            context.Log.Write(
                DisallowedInaccessibleIntrospectionField(
                    field,
                    type.Name,
                    schema));
        }
    }

    public void Handle(FieldArgumentEvent @event, CompositionContext context)
    {
        var (argument, field, type, schema) = @event;

        // Introspection arguments must be accessible.
        if (type.IsIntrospectionType && !ValidationHelper.IsAccessible(argument))
        {
            context.Log.Write(
                DisallowedInaccessibleIntrospectionArgument(
                    argument,
                    field.Name,
                    type.Name,
                    schema));
        }
    }

    public void Handle(DirectiveArgumentEvent @event, CompositionContext context)
    {
        var (argument, directive, schema) = @event;

        // Built-in directive arguments must be accessible.
        if (BuiltIns.IsBuiltInDirective(directive.Name) && !ValidationHelper.IsAccessible(argument))
        {
            context.Log.Write(
                DisallowedInaccessibleDirectiveArgument(
                    argument,
                    directive.Name,
                    schema));
        }
    }
}
