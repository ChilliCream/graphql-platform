using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Types;
using static HotChocolate.Configuration.Validation.TypeValidationHelper;
using static HotChocolate.Utilities.ErrorHelper;

namespace HotChocolate.Configuration.Validation;

/// <summary>
/// Implements directive type validation defined in the spec.
/// http://spec.graphql.org/draft/#sec-Type-System.Directives.Validation
/// </summary>
internal class DirectiveValidationRule : ISchemaValidationRule
{
    private const char _underscore = '_';

    public void Validate(
        IReadOnlyList<ITypeSystemObject> typeSystemObjects,
        IReadOnlySchemaOptions options,
        ICollection<ISchemaError> errors)
    {
        if (options.StrictValidation)
        {
            foreach (var type in typeSystemObjects.OfType<DirectiveType>())
            {
                EnsureDirectiveNameIsValid(type, errors);
                EnsureArgumentNamesAreValid(type, errors);
                EnsureArgumentDeprecationIsValid(type, errors);
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

            if (firstTwoLetters[0] == _underscore &&
                firstTwoLetters[1] == _underscore)
            {
                errors.Add(TwoUnderscoresNotAllowedOnDirectiveName(type));
            }
        }
    }
}
