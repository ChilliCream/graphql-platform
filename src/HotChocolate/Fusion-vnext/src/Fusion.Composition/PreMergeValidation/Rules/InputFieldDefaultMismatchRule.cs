using System.Collections.Immutable;
using HotChocolate.Fusion.Events;
using HotChocolate.Language;
using static HotChocolate.Fusion.Logging.LogEntryHelper;

namespace HotChocolate.Fusion.PreMergeValidation.Rules;

/// <summary>
/// <para>
/// Input fields in different source schemas that have the same name are required to have consistent
/// default values. This ensures that there is no ambiguity or inconsistency when merging input
/// fields from different source schemas.
/// </para>
/// <para>
/// A mismatch in default values for input fields with the same name across different source schemas
/// will result in a schema composition error.
/// </para>
/// </summary>
/// <seealso href="https://graphql.github.io/composite-schemas-spec/draft/#sec-Input-Field-Default-Mismatch">
/// Specification
/// </seealso>
internal sealed class InputFieldDefaultMismatchRule : IEventHandler<InputFieldGroupEvent>
{
    public void Handle(InputFieldGroupEvent @event, CompositionContext context)
    {
        var (_, fieldGroup, typeName) = @event;

        var fieldGroupWithDefaultValues = fieldGroup
            .Where(i => i.Field.DefaultValue is not null)
            .ToImmutableArray();

        var defaultValues = fieldGroupWithDefaultValues
            .Select(i => i.Field.DefaultValue!)
            .ToImmutableHashSet(SyntaxComparer.BySyntax);

        if (defaultValues.Count <= 1)
        {
            return;
        }

        for (var i = 0; i < fieldGroupWithDefaultValues.Length - 1; i++)
        {
            var (fieldA, _, schemaA) = fieldGroupWithDefaultValues[i];
            var (fieldB, _, schemaB) = fieldGroupWithDefaultValues[i + 1];
            var defaultValueA = fieldA.DefaultValue!;
            var defaultValueB = fieldB.DefaultValue!;

            if (!SyntaxComparer.BySyntax.Equals(defaultValueA, defaultValueB))
            {
                context.Log.Write(
                    InputFieldDefaultMismatch(
                        defaultValueA,
                        defaultValueB,
                        fieldA,
                        typeName,
                        schemaA,
                        schemaB));
            }
        }
    }
}
