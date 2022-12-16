using System;
using System.Collections.Generic;
using HotChocolate.Types;
using static HotChocolate.Configuration.Validation.TypeValidationHelper;

#nullable enable

namespace HotChocolate.Configuration.Validation;

internal sealed class InterfaceTypeValidationRule : ISchemaValidationRule
{
    public void Validate(
        ReadOnlySpan<ITypeSystemObject> typeSystemObjects,
        IReadOnlySchemaOptions options,
        ICollection<ISchemaError> errors)
    {
        if (options.StrictValidation)
        {
            foreach (var type in typeSystemObjects)
            {
                if (type is InterfaceType interfaceType)
                {
                    EnsureTypeHasFields(interfaceType, errors);
                    EnsureFieldNamesAreValid(interfaceType, errors);
                    EnsureInterfacesAreCorrectlyImplemented(interfaceType, errors);
                    EnsureArgumentDeprecationIsValid(interfaceType, errors);
                }
            }
        }
    }
}
