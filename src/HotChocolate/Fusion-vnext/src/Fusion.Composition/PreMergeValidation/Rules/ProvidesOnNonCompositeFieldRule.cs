using System.Collections.Immutable;
using HotChocolate.Fusion.Events;
using HotChocolate.Fusion.Extensions;
using HotChocolate.Skimmed;
using static HotChocolate.Fusion.Logging.LogEntryHelper;

namespace HotChocolate.Fusion.PreMergeValidation.Rules;

/// <summary>
/// The <c>@provides</c> directive allows a field to “provide” additional nested fields on the
/// composite type it returns. If a field’s base type is not an object or interface type (e.g.,
/// String, Int, Boolean, Enum, Union, or an Input type), it cannot hold nested fields for
/// <c>@provides</c> to select. Consequently, attaching <c>@provides</c> to such a field is
/// invalid and raises a PROVIDES_ON_NON_OBJECT_FIELD error.
/// </summary>
/// <seealso href="https://graphql.github.io/composite-schemas-spec/draft/#sec-Provides-on-Non-Composite-Field">
/// Specification
/// </seealso>
internal sealed class ProvidesOnNonCompositeFieldRule : IEventHandler<OutputFieldEvent>
{
    public void Handle(OutputFieldEvent @event, CompositionContext context)
    {
        var (field, _, schema) = @event;

        if (ValidationHelper.HasProvidesDirective(field))
        {
            var fieldType = field.Type.InnerNullableType();
            while (fieldType is not INamedTypeDefinition)
            {
                fieldType = fieldType.InnerNullableType();
            }

            if (fieldType is not InterfaceTypeDefinition and not ObjectTypeDefinition)
            {
                context.Log.Write(
                    ProvidesOnNonCompositeField(field, (INamedTypeDefinition)fieldType, schema));
            }
        }
    }
}
