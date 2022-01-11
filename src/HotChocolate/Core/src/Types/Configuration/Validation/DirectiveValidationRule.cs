using System.Collections.Generic;
using System.Linq;
using HotChocolate.Types;
using static HotChocolate.Configuration.Validation.TypeValidationHelper;

namespace HotChocolate.Configuration.Validation;

/// <summary>
/// Implements directive type validation defined in the spec.
/// http://spec.graphql.org/draft/#sec-Type-System.Directives.Validation
/// </summary>
internal class DirectiveValidationRule : ISchemaValidationRule
{
    public void Validate(
        IReadOnlyList<ITypeSystemObject> typeSystemObjects,
        IReadOnlySchemaOptions options,
        ICollection<ISchemaError> errors)
    {
        if (options.StrictValidation)
        {
            foreach (DirectiveType type in typeSystemObjects.OfType<DirectiveType>())
            {
                EnsureDirectiveNameIsValid(type, errors);
                EnsureArgumentNamesAreValid(type, errors);
                EnsureArgumentDeprecationIsValid(type, errors);
            }
        }
    }
}
