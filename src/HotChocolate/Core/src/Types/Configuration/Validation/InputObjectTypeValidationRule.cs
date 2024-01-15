#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;
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
        if (!options.StrictValidation)
        {
            return;
        }

        List<string>? names = null;
        CycleValidationContext cycleValidationContext = new()
        {
            CycleStartIndex = new(),
            Errors = errors,
            FieldPath = new(),
        };
        foreach (var type in typeSystemObjects)
        {
            if (type is not InputObjectType inputType)
            {
                continue;
            }

            EnsureTypeHasFields(inputType, errors);
            EnsureFieldNamesAreValid(inputType, errors);
            EnsureOneOfFieldsAreValid(inputType, errors, ref names);
            EnsureFieldDeprecationIsValid(inputType, errors);
            TryReachSelfRecursively(inputType, cycleValidationContext);

            cycleValidationContext.CycleStartIndex.Clear();
        }
    }

    private struct CycleValidationContext
    {
        public Dictionary<InputObjectType, int> CycleStartIndex { get; set; }
        public ICollection<ISchemaError> Errors { get; set; }
        public List<string> FieldPath { get; set; }
    }

    // Note that this algorithm is not optimal.
    // It doesn't cache explored paths in exiting nodes.
    private static void TryReachSelfRecursively(
        InputObjectType type,
        in CycleValidationContext cycleValidationContext)
    {
        cycleValidationContext.CycleStartIndex[type] = cycleValidationContext.FieldPath.Count;

        foreach (var field in type.Fields)
        {
            static (bool IsRequired, IType NestedType) UnwrapCompletely(IType type)
            {
                bool isRequired = false;
                while (true)
                {
                    if (type is NonNullType nonNullType)
                    {
                        type = nonNullType.Type;
                        isRequired = true;
                    }
                    else if (type is ListType listType)
                    {
                        type = listType.ElementType;
                        isRequired = true;
                    }
                    else
                    {
                        return (isRequired, type);
                    }
                }
            }

            var (isRequired, unwrappedType) = UnwrapCompletely(field.Type);
            if (!isRequired ||
                unwrappedType is not InputObjectType inputObjectType)
            {
                continue;
            }

            cycleValidationContext.FieldPath.Add(field.Name);
            if (cycleValidationContext.CycleStartIndex.TryGetValue(inputObjectType, out var cycleIndex))
            {
                var cyclePath = cycleValidationContext.FieldPath.Skip(cycleIndex);
                cycleValidationContext.Errors.Add(
                    InputObjectMustNotHaveRecursiveNonNullableReferencesToSelf(type, cyclePath));
            }
            else
            {
                TryReachSelfRecursively(inputObjectType, cycleValidationContext);
            }
            cycleValidationContext.FieldPath.Pop();
        }
    }

    private static void EnsureOneOfFieldsAreValid(
        InputObjectType type,
        ICollection<ISchemaError> errors,
        ref List<string>? temp)
    {
        if (!type.Directives.ContainsDirective(WellKnownDirectives.OneOf))
        {
            return;
        }

        temp ??= new List<string>();

        foreach (var field in type.Fields)
        {
            if (field.Type.Kind is TypeKind.NonNull || field.DefaultValue is not null)
            {
                temp.Add(field.Name);
            }
        }

        if (temp.Count == 0)
        {
            return;
        }

        var fieldNames = new string[temp.Count];
        for (var i = 0; i < temp.Count; i++)
        {
            fieldNames[i] = temp[i];
        }

        temp.Clear();
        errors.Add(OneofInputObjectMustHaveNullableFieldsWithoutDefaults(type, fieldNames));
    }
}
