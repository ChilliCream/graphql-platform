using System.Collections.Immutable;
using HotChocolate.Fusion.Events;
using HotChocolate.Fusion.Events.Contracts;
using HotChocolate.Fusion.Extensions;
using HotChocolate.Types;
using static HotChocolate.Fusion.Logging.LogEntryHelper;

namespace HotChocolate.Fusion.PreMergeValidationRules;

/// <summary>
/// <para>
/// When merging a field definition across multiple schemas, any argument that is non-null (i.e.,
/// “required”) in one schema must appear in all schemas that define that field. In other words,
/// arguments are effectively merged by intersection: if an argument is considered required in any
/// schema, then that same argument must exist in every schema that contributes to the composite
/// definition. If a required argument is missing in one schema, there is no consistent way to
/// define that field across schemas.
/// </para>
/// <para>
/// If an argument is marked with <c>@require</c>, it is treated as non-required. Consequently, this
/// argument must either be nullable in all other schemas or must also be marked with
/// <c>@require</c> in all other schemas.
/// </para>
/// </summary>
/// <seealso href="https://graphql.github.io/composite-schemas-spec/draft/#sec-Field-With-Missing-Required-Arguments">
/// Specification
/// </seealso>
internal sealed class FieldWithMissingRequiredArgumentRule : IEventHandler<OutputFieldGroupEvent>
{
    public void Handle(OutputFieldGroupEvent @event, CompositionContext context)
    {
        var fieldGroup = @event.FieldGroup;

        var requiredArgumentNames =
            fieldGroup.SelectMany(
                i => i
                    .Field
                    .Arguments
                    .AsEnumerable()
                    .Where(a => a.Type is NonNullType && !a.HasRequireDirective())
                    .Select(a => a.Name))
                .ToImmutableHashSet();

        foreach (var (field, type, schema) in fieldGroup)
        {
            foreach (var requiredArgumentName in requiredArgumentNames)
            {
                var hasRequiredArgument =
                    field.Arguments
                        .AsEnumerable()
                        .Any(a => a.Name == requiredArgumentName && !a.HasRequireDirective());

                if (!hasRequiredArgument)
                {
                    context.Log.Write(
                        FieldWithMissingRequiredArgument(
                            requiredArgumentName,
                            field,
                            type.Name,
                            schema));
                }
            }
        }
    }
}
