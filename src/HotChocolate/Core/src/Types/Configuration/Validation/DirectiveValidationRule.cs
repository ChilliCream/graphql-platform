using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using static HotChocolate.Configuration.Validation.TypeValidationHelper;
using static HotChocolate.Utilities.ErrorHelper;

namespace HotChocolate.Configuration.Validation;

/// <summary>
/// Implements directive type validation defined in the spec.
/// https://spec.graphql.org/draft/#sec-Type-System.Directives.Validation
/// </summary>
internal sealed class DirectiveValidationRule : ISchemaValidationRule
{
    private const char PrefixCharacter = '_';

    public void Validate(
        IDescriptorContext context,
        ISchemaDefinition schema,
        ICollection<ISchemaError> errors)
    {
        if (context.Options.StrictValidation)
        {
            foreach (var directiveDefinition in schema.DirectiveDefinitions)
            {
                EnsureDirectiveNameIsValid(directiveDefinition, errors);
                EnsureArgumentNamesAreValid(directiveDefinition, errors);
                EnsureArgumentDeprecationIsValid(directiveDefinition, errors);
            }
        }
    }

    private static void EnsureDirectiveNameIsValid(
        IDirectiveDefinition type,
        ICollection<ISchemaError> errors)
    {
        if (type.Name.Length > 2)
        {
            var firstTwoLetters = type.Name.AsSpan()[..2];

            if (firstTwoLetters[0] == PrefixCharacter
                && firstTwoLetters[1] == PrefixCharacter)
            {
                errors.Add(TwoUnderscoresNotAllowedOnDirectiveName(type));
            }
        }
    }
}
