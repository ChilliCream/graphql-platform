using HotChocolate.Fusion.Events;
using HotChocolate.Fusion.Events.Contracts;
using HotChocolate.Fusion.Extensions;
using HotChocolate.Fusion.Info;
using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using static HotChocolate.Fusion.Logging.LogEntryHelper;

namespace HotChocolate.Fusion.PostMergeValidationRules;

/// <summary>
/// When an input field is declared as non-null in any source schema, it imposes a hard requirement:
/// queries or mutations that reference this field <i>must</i> provide a value for it. If the field
/// is then marked as <c>@inaccessible</c> or removed during schema composition, the final schema
/// would still implicitly demand a value for a field that no longer exists in the composed schema,
/// making it impossible to fulfill the requirement.
/// </summary>
/// <seealso href="https://graphql.github.io/composite-schemas-spec/draft/#sec-Non-Null-Input-Fields-cannot-be-inaccessible">
/// Specification
/// </seealso>
internal sealed class NonNullInputFieldIsInaccessibleRule : IEventHandler<SchemaEvent>
{
    public void Handle(SchemaEvent @event, CompositionContext context)
    {
        var schema = @event.Schema;

        var inputFieldGroup =
            context.SchemaDefinitions
                .SelectMany(
                    s => s.Types
                        .OfType<MutableInputObjectTypeDefinition>()
                        .SelectMany(
                            t => t.Fields.AsEnumerable().Select(f => new InputFieldInfo(f, t, s))));

        foreach (var (sourceField, sourceType, sourceSchema) in inputFieldGroup)
        {
            if (sourceField.Type is not NonNullType)
            {
                continue;
            }

            var coordinate = new SchemaCoordinate(sourceType.Name, sourceField.Name);

            if (!schema.TryGetMember(coordinate, out var member)
                || member is not MutableInputFieldDefinition inputField
                || inputField.HasFusionInaccessibleDirective())
            {
                context.Log.Write(
                    NonNullInputFieldIsInaccessible(
                        sourceField,
                        coordinate,
                        sourceSchema));
            }
        }
    }
}
