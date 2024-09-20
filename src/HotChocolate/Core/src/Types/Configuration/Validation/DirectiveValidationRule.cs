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
    private const char _prefixCharacter = '_';

    public void Validate(
        IDescriptorContext context,
        ISchema schema,
        ICollection<ISchemaError> errors)
    {
        if (context.Options.StrictValidation)
        {
            foreach (var directiveType in schema.DirectiveTypes)
            {
                EnsureDirectiveNameIsValid(directiveType, errors);
                EnsureArgumentNamesAreValid(directiveType, errors);
                EnsureArgumentDeprecationIsValid(directiveType, errors);
            }
        }
    }

    private static void EnsureDirectiveNameIsValid(
        DirectiveType type,
        ICollection<ISchemaError> errors)
    {
        if (type.Name.Length > 2)
        {
            var firstTwoLetters = type.Name.AsSpan().Slice(0, 2);

            if (firstTwoLetters[0] == _prefixCharacter &&
                firstTwoLetters[1] == _prefixCharacter)
            {
                errors.Add(TwoUnderscoresNotAllowedOnDirectiveName(type));
            }
        }
    }
}
