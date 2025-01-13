using HotChocolate.Fusion.Events;
using HotChocolate.Skimmed;
using static HotChocolate.Fusion.Logging.LogEntryHelper;

namespace HotChocolate.Fusion.SourceSchemaValidation.Rules;

/// <summary>
/// The <c>@key</c> directive is used to define the set of fields that uniquely identify an entity.
/// These fields must reference scalars or object types to ensure a valid and consistent
/// representation of the entity across subgraphs. Fields of types <c>List</c>, <c>Interface</c>, or
/// <c>Union</c> cannot be part of a <c>@key</c> because they do not have a well-defined unique
/// value.
/// </summary>
/// <seealso href="https://graphql.github.io/composite-schemas-spec/draft/#sec-Key-Fields-Select-Invalid-Type">
/// Specification
/// </seealso>
internal sealed class KeyFieldsSelectInvalidTypeRule : IEventHandler<KeyFieldEvent>
{
    public void Handle(KeyFieldEvent @event, CompositionContext context)
    {
        var (keyDirective, entityType, field, type, schema) = @event;

        var fieldType = field.Type.NullableType();

        if (fieldType is InterfaceTypeDefinition or ListTypeDefinition or UnionTypeDefinition)
        {
            context.Log.Write(
                KeyFieldsSelectInvalidType(
                    entityType.Name,
                    keyDirective,
                    field.Name,
                    type.Name,
                    schema));
        }
    }
}
