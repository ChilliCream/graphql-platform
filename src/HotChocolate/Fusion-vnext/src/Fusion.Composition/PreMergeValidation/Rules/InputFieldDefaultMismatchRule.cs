using HotChocolate.Fusion.Events;
using HotChocolate.Language;
using HotChocolate.Skimmed;
using static HotChocolate.Fusion.Logging.LogEntryHelper;

namespace HotChocolate.Fusion.PreMergeValidation.Rules;

/// <summary>
/// Input fields in different source schemas that have the same name are required to have
/// consistent default values. This ensures that there is no ambiguity or inconsistency
/// when merging input fields from different source schemas.
/// <br />
/// A mismatch in default values for input fields with the same name across different source
/// schemas will result in a schema composition error.
/// </summary>
/// <seealso href="https://graphql.github.io/composite-schemas-spec/draft/#sec-External-Unused">
/// Specification
/// </seealso>
internal sealed class InputFieldDefaultMismatchRule : IEventHandler<TypeGroupEvent>
{
    public void Handle(TypeGroupEvent @event, CompositionContext context)
    {
        var (typeName, typeInfos) = @event;

        if (typeInfos.Any(i => i.Type is not InputObjectTypeDefinition))
        {
            return; // Different shape caught elsewhere.
        }

        var defaultValuesByField = typeInfos
            .SelectMany(
                i => ((InputObjectTypeDefinition)i.Type).Fields,
                (_, f) => (f.Name, f.DefaultValue))
            .ToLookup(f => f.Name, f => f.DefaultValue);

        foreach (var field in defaultValuesByField)
        {
            var defaultValue = field.First();

            if (field.Skip(1).Any(dv => !SyntaxComparer.BySyntax.Equals(dv, defaultValue)))
            {
                context.Log.Write(
                    InputFieldDefaultMismatch(field.Key, typeName));
            }
        }
    }
}
