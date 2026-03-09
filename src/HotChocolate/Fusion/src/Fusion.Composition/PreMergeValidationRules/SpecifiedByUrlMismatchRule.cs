using HotChocolate.Fusion.Events;
using HotChocolate.Fusion.Events.Contracts;
using static HotChocolate.Fusion.Logging.LogEntryHelper;

namespace HotChocolate.Fusion.PreMergeValidationRules;

/// <summary>
/// <para>
/// This validation rule checks for mismatches in the specified-by URLs of scalar types across
/// different schemas. If a scalar type is defined in multiple schemas with different specified-by
/// URLs, a warning is logged to indicate the inconsistency. This helps ensure that clients
/// consuming the merged schema have a consistent understanding of the scalar type's behavior and
/// specifications, even if the underlying implementations differ across schemas.
/// </para>
/// </summary>
/// <seealso href="https://graphql.github.io/composite-schemas-spec/draft/#sec-SpecifiedBy-URL-Mismatch">
/// Specification
/// </seealso>
internal sealed class SpecifiedByUrlMismatchRule : IEventHandler<ScalarTypeGroupEvent>
{
    public void Handle(ScalarTypeGroupEvent @event, CompositionContext context)
    {
        var (_, typeGroup) = @event;

        for (var i = 0; i < typeGroup.Length - 1; i++)
        {
            var typeInfoA = typeGroup[i];
            var typeInfoB = typeGroup[i + 1];
            var specifiedByA = typeInfoA.Scalar.SpecifiedBy;
            var specifiedByB = typeInfoB.Scalar.SpecifiedBy;

            if (specifiedByA != specifiedByB)
            {
                context.Log.Write(
                    SpecifiedByUrlMismatch(
                        typeInfoA.Scalar,
                        typeInfoA.Schema,
                        specifiedByA?.ToString(),
                        typeInfoB.Schema,
                        specifiedByB?.ToString()));
            }
        }
    }
}
