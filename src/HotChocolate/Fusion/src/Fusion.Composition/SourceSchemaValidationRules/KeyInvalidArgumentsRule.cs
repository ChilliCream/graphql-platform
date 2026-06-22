using System.Collections.Immutable;
using HotChocolate.Fusion.Events;
using HotChocolate.Fusion.Events.Contracts;
using HotChocolate.Fusion.Extensions;
using HotChocolate.Fusion.Validators;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using static HotChocolate.Fusion.Logging.LogEntryHelper;
using static HotChocolate.Fusion.WellKnownArgumentNames;
using static HotChocolate.Language.Utf8GraphQLParser.Syntax;

namespace HotChocolate.Fusion.SourceSchemaValidationRules;

/// <summary>
/// Fields included in the <c>fields</c> argument of the <c>@key</c> directive may accept arguments,
/// provided the supplied values are constant literals that match the field's argument definitions.
/// Each argument must be defined on the field, its value must be compatible with the argument type,
/// and every required argument must be supplied.
/// </summary>
/// <seealso href="https://graphql.github.io/composite-schemas-spec/draft/#sec-Key-Invalid-Arguments">
/// Specification
/// </seealso>
internal sealed class KeyInvalidArgumentsRule : IEventHandler<ComplexTypeEvent>
{
    public void Handle(ComplexTypeEvent @event, CompositionContext context)
    {
        var (complexType, schema) = @event;

        foreach (var (keyDirective, keyInfo) in complexType.KeyInfoByDirective)
        {
            if (keyInfo.IsInvalidFieldsSyntax || keyInfo.IsInvalidFieldsType)
            {
                continue;
            }

            var fieldsArgument = (string)keyDirective.Arguments[Fields].Value!;
            var selectionSet = ParseSelectionSet($"{{ {fieldsArgument} }}");
            var errors = new List<string>();

            ValidateArguments(selectionSet, complexType, errors);

            if (errors.Count > 0)
            {
                context.Log.Write(
                    KeyInvalidArguments(keyDirective, complexType, schema, [.. errors]));
            }
        }
    }

    private static void ValidateArguments(
        SelectionSetNode selectionSet,
        IComplexTypeDefinition type,
        List<string> errors)
    {
        foreach (var selection in selectionSet.Selections)
        {
            if (selection is not FieldNode fieldNode)
            {
                continue;
            }

            if (!type.Fields.TryGetField(fieldNode.Name.Value, out var field))
            {
                // Unknown fields are reported by KeyInvalidFieldsRule.
                continue;
            }

            ConstantArgumentValidator.Validate(
                fieldNode.Arguments,
                field,
                field.Coordinate.ToString(),
                errors);

            if (fieldNode.SelectionSet is not null
                && field.Type.NullableType() is IComplexTypeDefinition childType)
            {
                ValidateArguments(fieldNode.SelectionSet, childType, errors);
            }
        }
    }
}
