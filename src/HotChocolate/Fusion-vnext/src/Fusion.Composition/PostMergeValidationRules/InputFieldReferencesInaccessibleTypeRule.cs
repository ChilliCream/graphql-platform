using HotChocolate.Fusion.Events;
using HotChocolate.Fusion.Events.Contracts;
using HotChocolate.Fusion.Extensions;
using HotChocolate.Skimmed;
using static HotChocolate.Fusion.Logging.LogEntryHelper;

namespace HotChocolate.Fusion.PostMergeValidationRules;

/// <summary>
/// In a composed schema, a field within an input type must only reference types that are exposed.
/// This requirement guarantees that public types do not reference <c>inaccessible</c> structures
/// which are intended for internal use.
/// </summary>
/// <seealso href="https://graphql.github.io/composite-schemas-spec/draft/#sec-Input-Fields-cannot-reference-inaccessible-type">
/// Specification
/// </seealso>
internal sealed class InputFieldReferencesInaccessibleTypeRule : IEventHandler<InputFieldEvent>
{
    public void Handle(InputFieldEvent @event, CompositionContext context)
    {
        var (field, type, schema) = @event;

        if (field.HasInaccessibleDirective())
        {
            return;
        }

        var namedType = field.Type.NamedType();

        if (namedType.HasInaccessibleDirective())
        {
            context.Log.Write(
                InputFieldReferencesInaccessibleType(
                    field,
                    type.Name,
                    namedType.Name,
                    schema));
        }
    }
}
