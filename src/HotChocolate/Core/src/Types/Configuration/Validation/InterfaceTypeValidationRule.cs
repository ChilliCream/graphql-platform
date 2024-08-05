using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using static HotChocolate.Configuration.Validation.TypeValidationHelper;

#nullable enable

namespace HotChocolate.Configuration.Validation;

internal sealed class InterfaceTypeValidationRule : ISchemaValidationRule
{
    public void Validate(
        IDescriptorContext context,
        ISchema schema,
        ICollection<ISchemaError> errors)
    {
        if (context.Options.StrictValidation)
        {
            foreach (var type in schema.Types)
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
