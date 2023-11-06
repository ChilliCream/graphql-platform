#nullable enable

using System;
using System.Collections.Generic;
using HotChocolate.Types;
using static HotChocolate.Configuration.Validation.TypeValidationHelper;
using static HotChocolate.Utilities.ErrorHelper;

namespace HotChocolate.Configuration.Validation;

internal sealed class InputObjectTypeValidationRule : ISchemaValidationRule
{
    public void Validate(
        ReadOnlySpan<ITypeSystemObject> typeSystemObjects,
        IReadOnlySchemaOptions options,
        ICollection<ISchemaError> errors)
    {
        if (options.StrictValidation)
        {
            List<string>? names = null;

            foreach (var type in typeSystemObjects)
            {
                if (type is InputObjectType inputType)
                {
                    EnsureTypeHasFields(inputType, errors);
                    EnsureFieldNamesAreValid(inputType, errors);
                    EnsureOneOfFieldsAreValid(inputType, errors, ref names);
                    EnsureFieldDeprecationIsValid(inputType, errors);
                }
            }
        }
    }

    private static void EnsureOneOfFieldsAreValid(
        InputObjectType type,
        ICollection<ISchemaError> errors,
        ref List<string>? temp)
    {
        if (type.Directives.ContainsDirective(WellKnownDirectives.OneOf))
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
