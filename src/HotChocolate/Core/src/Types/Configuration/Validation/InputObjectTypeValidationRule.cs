#nullable enable

using System.Collections.Generic;
using System.Linq;
using HotChocolate.Types;
using static HotChocolate.Configuration.Validation.TypeValidationHelper;
using static HotChocolate.Utilities.ErrorHelper;

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
            List<string>? names = null;

            foreach (var type in typeSystemObjects.OfType<InputObjectType>())
            {
                EnsureTypeHasFields(type, errors);
                EnsureFieldNamesAreValid(type, errors);
                EnsureOneOfFieldsAreValid(type, errors, ref names);
                EnsureFieldDeprecationIsValid(type, errors);
            }
        }
    }

    private void EnsureOneOfFieldsAreValid(
        InputObjectType type,
        ICollection<ISchemaError> errors,
        ref List<string>? temp)
    {
        if (type.Directives.Contains(WellKnownDirectives.OneOf))
        {
            temp ??= new List<string>();

            foreach (var field in type.Fields)
            {
                if (field.Type.Kind is TypeKind.NonNull || field.DefaultValue is not null)
                {
                    temp.Add(field.Name);
                }
            }

            if (temp.Count > 0)
            {
                var fieldNames = new string[temp.Count];

                for (var i = 0; i < temp.Count; i++)
                {
                    fieldNames[i] = temp[i];
                }

                temp.Clear();
                errors.Add(OneofInputObjectMustHaveNullableFieldsWithoutDefaults(type, fieldNames));
            }
        }
    }
}
