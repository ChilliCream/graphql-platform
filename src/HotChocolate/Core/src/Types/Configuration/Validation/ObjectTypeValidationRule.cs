using System.Collections.Generic;
using System.Linq;
using HotChocolate.Types;
using static HotChocolate.Configuration.Validation.TypeValidationHelper;

#nullable enable

namespace HotChocolate.Configuration.Validation;

/// <summary>
/// Implements the object type validation defined in the spec.
/// http://spec.graphql.org/draft/#sec-Objects.Type-Validation
/// </summary>
internal class ObjectTypeValidationRule : ISchemaValidationRule
{
    public void Validate(
        IReadOnlyList<ITypeSystemObject> typeSystemObjects,
        IReadOnlySchemaOptions options,
        ICollection<ISchemaError> errors)
    {
        if (options.StrictValidation)
        {
            foreach (var type in typeSystemObjects.OfType<ObjectType>())
            {
                EnsureTypeHasFields(type, errors);
                EnsureFieldNamesAreValid(type, errors);
                EnsureInterfacesAreCorrectlyImplemented(type, errors);
                EnsureArgumentDeprecationIsValid(type, errors);
            }
        }
    }
}
