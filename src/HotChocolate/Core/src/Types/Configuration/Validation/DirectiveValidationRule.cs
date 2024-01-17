using System;
using System.Collections.Generic;
using HotChocolate.Types;
using static HotChocolate.Configuration.Validation.TypeValidationHelper;
using static HotChocolate.Utilities.ErrorHelper;

namespace HotChocolate.Configuration.Validation;

/// <summary>
/// Implements directive type validation defined in the spec.
/// http://spec.graphql.org/draft/#sec-Type-System.Directives.Validation
/// </summary>
internal sealed class DirectiveValidationRule : ISchemaValidationRule
{
    private const char _prefixCharacter = '_';

    public void Validate(
        ReadOnlySpan<ITypeSystemObject> typeSystemObjects,
        IReadOnlySchemaOptions options,
        ICollection<ISchemaError> errors)
    {
        if (options.StrictValidation)
        {
            foreach (var type in typeSystemObjects)
            {
                if (type is DirectiveType directiveType)
                {
                    EnsureDirectiveNameIsValid(directiveType, errors);
                    EnsureArgumentNamesAreValid(directiveType, errors);
                    EnsureArgumentDeprecationIsValid(directiveType, errors);
                }
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
