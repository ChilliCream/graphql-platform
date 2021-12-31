using System.Collections.Generic;
using System.Linq;
using HotChocolate.Types;
using static HotChocolate.Configuration.Validation.TypeValidationHelper;
using static HotChocolate.Configuration.Validation.ErrorHelper;

namespace HotChocolate.Configuration.Validation;

public class InputObjectTypeValidationRule : ISchemaValidationRule
{
    public void Validate(
        IReadOnlyList<ITypeSystemObject> typeSystemObjects,
        IReadOnlySchemaOptions options,
        ICollection<ISchemaError> errors)
    {
        if (options.StrictValidation)
        {
            foreach (InputObjectType type in typeSystemObjects.OfType<InputObjectType>())
            {
                EnsureTypeHasFields(type, errors);
                EnsureFieldNamesAreValid(type, errors);
                EnsureOneOfFieldsAreValid(type, errors);
            }
        }
    }

    private static void EnsureOneOfFieldsAreValid(
        InputObjectType type,
        ICollection<ISchemaError> errors)
    {
        if (type.Directives.Contains(WellKnownDirectives.OneOf))
        {
            if (type.Fields.Any(t => t.Type.Kind is TypeKind.NonNull || t.DefaultValue is not null))
            {
                string[] fieldNames = type.Fields
                    .Where(t => t.Type.Kind is TypeKind.NonNull || t.DefaultValue is not null)
                    .Select(t => t.Name.Value)
                    .ToArray();

                errors.Add(OneofInputObjectMustHaveNullableFieldsWithoutDefaults(type, fieldNames));
            }
        }
    }
}
