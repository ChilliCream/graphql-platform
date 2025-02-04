using System.Collections.Immutable;
using System.Diagnostics;
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
internal sealed class InputFieldReferencesInaccessibleTypeRule
    : IEventHandler<SchemaEvent>, IEventHandler<InputFieldEvent>
{
    private ImmutableHashSet<string>? _inaccessibleTypes;

    public void Handle(SchemaEvent @event, CompositionContext context)
    {
        var builder = ImmutableHashSet.CreateBuilder<string>();
        foreach (var type in @event.Schema.Types)
        {
            if (type.HasInaccessibleDirective())
            {
                builder.Add(type.Name);
            }
        }

        _inaccessibleTypes = builder.ToImmutable();
    }

    public void Handle(InputFieldEvent @event, CompositionContext context)
    {
        Debug.Assert(_inaccessibleTypes is not null);

        var (field, type, schema) = @event;

        if (field.HasInaccessibleDirective())
        {
            return;
        }

        var fieldTypeName = field.Type.NamedType().Name;
        if (_inaccessibleTypes.Contains(fieldTypeName))
        {
            context.Log.Write(
                InputFieldReferencesInaccessibleType(
                    field,
                    type.Name,
                    fieldTypeName,
                    schema));
        }
    }
}
