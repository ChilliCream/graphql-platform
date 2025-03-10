using HotChocolate.Fusion.Events;
using HotChocolate.Fusion.Events.Contracts;
using HotChocolate.Fusion.Extensions;
using HotChocolate.Types.Mutable;
using static HotChocolate.Fusion.Logging.LogEntryHelper;

namespace HotChocolate.Fusion.SourceSchemaValidationRules;

/// <summary>
/// This rule ensures that every field marked as <c>@external</c> in a source schema is actually
/// used by that source schema in a <c>@provides</c> directive.
/// </summary>
/// <seealso href="https://graphql.github.io/composite-schemas-spec/draft/#sec-External-Unused">
/// Specification
/// </seealso>
internal sealed class ExternalUnusedRule : IEventHandler<OutputFieldEvent>
{
    public void Handle(OutputFieldEvent @event, CompositionContext context)
    {
        var (field, type, schema) = @event;

        if (field.HasExternalDirective())
        {
            var referencingFields =
                schema.Types
                    .OfType<MutableComplexTypeDefinition>()
                    .SelectMany(t => t.Fields.AsEnumerable())
                    .Where(f => f.Type == type);

            var isReferenced =
                referencingFields.Any(f => ValidationHelper.ProvidesFieldName(f, field.Name));

            if (!isReferenced)
            {
                context.Log.Write(ExternalUnused(field, type, schema));
            }
        }
    }
}
