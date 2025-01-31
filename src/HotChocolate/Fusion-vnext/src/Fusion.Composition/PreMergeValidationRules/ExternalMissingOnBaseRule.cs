using System.Collections.Immutable;
using HotChocolate.Fusion.Events;
using HotChocolate.Fusion.Events.Contracts;
using HotChocolate.Fusion.Extensions;
using static HotChocolate.Fusion.Logging.LogEntryHelper;

namespace HotChocolate.Fusion.PreMergeValidationRules;

/// <summary>
/// This rule ensures that any field marked as <c>@external</c> in a source schema is actually
/// defined (non-<c>@external</c>) in at least one other source schema. The <c>@external</c>
/// directive is used to indicate that the field is not usually resolved by the source schema it is
/// declared in, implying it should be resolvable by at least one other source schema.
/// </summary>
/// <seealso href="https://graphql.github.io/composite-schemas-spec/draft/#sec-External-Missing-on-Base">
/// Specification
/// </seealso>
internal sealed class ExternalMissingOnBaseRule : IEventHandler<OutputFieldGroupEvent>
{
    public void Handle(OutputFieldGroupEvent @event, CompositionContext context)
    {
        var fieldGroup = @event.FieldGroup;

        var externalFields = fieldGroup
            .Where(i => i.Field.HasExternalDirective())
            .ToImmutableArray();

        var nonExternalFieldCount = fieldGroup.Length - externalFields.Length;

        foreach (var (field, type, schema) in externalFields)
        {
            if (nonExternalFieldCount == 0)
            {
                context.Log.Write(ExternalMissingOnBase(field, type, schema));
            }
        }
    }
}
