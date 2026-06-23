using HotChocolate.Fusion.Events;
using HotChocolate.Fusion.Events.Contracts;
using HotChocolate.Fusion.Extensions;
using HotChocolate.Fusion.Validators;
using HotChocolate.Language;
using HotChocolate.Types;
using static HotChocolate.Fusion.Logging.LogEntryHelper;
using static HotChocolate.Fusion.WellKnownArgumentNames;
using static HotChocolate.Language.Utf8GraphQLParser.Syntax;

namespace HotChocolate.Fusion.SourceSchemaValidationRules;

/// <summary>
/// The <c>@key</c> directive is used to define the set of fields that uniquely identify an entity.
/// Fields included in the <c>fields</c> argument of the <c>@key</c> directive may accept arguments,
/// provided the supplied values are constant literals — variables are not permitted, since a key
/// must be statically resolvable from the schema alone. The constants must satisfy the field’s
/// argument definitions: argument names must be defined on the field, values must be coercible to
/// the corresponding argument types, and required arguments without defaults must be supplied.
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
                errors);

            if (fieldNode.SelectionSet is not null
                && field.Type.NullableType() is IComplexTypeDefinition childType)
            {
                ValidateArguments(fieldNode.SelectionSet, childType, errors);
            }
        }
    }
}
