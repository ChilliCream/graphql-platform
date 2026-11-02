using HotChocolate.Fusion.Events;
using HotChocolate.Fusion.Events.Contracts;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using static HotChocolate.Fusion.Logging.LogEntryHelper;
using ArgumentNames = HotChocolate.Fusion.WellKnownArgumentNames;
using DirectiveNames = HotChocolate.Fusion.WellKnownDirectiveNames;

namespace HotChocolate.Fusion.SourceSchemaValidationRules;

/// <summary>
/// Reports <c>@requiresScopes</c> and <c>@policy</c> directives whose argument is missing, null,
/// not a string, a list of strings or a list of string lists, or contains an empty list, an empty
/// group or a blank string.
/// </summary>
internal sealed class AuthorizationDirectiveArgumentRule
    : IEventHandler<OutputFieldEvent>
    , IEventHandler<TypeEvent>
{
    public void Handle(OutputFieldEvent @event, CompositionContext context)
    {
        var (field, _, schema) = @event;

        Validate(field, field.Coordinate, schema, context);
    }

    public void Handle(TypeEvent @event, CompositionContext context)
    {
        var (type, schema) = @event;

        switch (type)
        {
            case MutableObjectTypeDefinition objectType:
                Validate(objectType, objectType.Coordinate, schema, context);
                break;

            case MutableInterfaceTypeDefinition interfaceType:
                Validate(interfaceType, interfaceType.Coordinate, schema, context);
                break;

            case MutableEnumTypeDefinition enumType:
                Validate(enumType, enumType.Coordinate, schema, context);
                break;

            case MutableScalarTypeDefinition scalarType:
                Validate(scalarType, scalarType.Coordinate, schema, context);
                break;
        }
    }

    private static void Validate(
        IDirectivesProvider member,
        SchemaCoordinate coordinate,
        MutableSchemaDefinition schema,
        CompositionContext context)
    {
        foreach (var directive in member.Directives)
        {
            var argumentName = directive.Name switch
            {
                DirectiveNames.RequiresScopes => ArgumentNames.Scopes,
                DirectiveNames.Policy => ArgumentNames.Policies,
                _ => null
            };

            if (argumentName is null)
            {
                continue;
            }

            directive.Arguments.TryGetValue(argumentName, out var value);

            if (!IsValid(value))
            {
                context.Log.Write(
                    AuthorizationDirectiveArgumentInvalid(
                        directive.Name,
                        argumentName,
                        value,
                        coordinate,
                        schema));
            }
        }
    }

    // GraphQL input coercion permits a single string wherever a list of string lists is expected.
    private static bool IsValid(IValueNode? value)
    {
        switch (value)
        {
            case StringValueNode single:
                return !string.IsNullOrWhiteSpace(single.Value);

            case ListValueNode { Items.Count: > 0 } list:
                foreach (var item in list.Items)
                {
                    switch (item)
                    {
                        case StringValueNode factor when !string.IsNullOrWhiteSpace(factor.Value):
                            continue;

                        case ListValueNode { Items.Count: > 0 } group:
                            foreach (var scope in group.Items)
                            {
                                if (scope is not StringValueNode { Value: var text }
                                    || string.IsNullOrWhiteSpace(text))
                                {
                                    return false;
                                }
                            }

                            continue;

                        default:
                            return false;
                    }
                }

                return true;

            default:
                return false;
        }
    }
}
