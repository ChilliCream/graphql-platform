using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using static HotChocolate.Utilities.ErrorHelper;

namespace HotChocolate.Configuration.Validation;

internal sealed class RequiresOptInValidationRule : ISchemaValidationRule
{
    public void Validate(
        IDescriptorContext context,
        ISchemaDefinition schema,
        ICollection<ISchemaError> errors)
    {
        if (!context.Options.EnableOptInFeatures)
        {
            return;
        }

        foreach (var type in schema.Types)
        {
            switch (type)
            {
                case IInputObjectTypeDefinition inputObjectType:
                    foreach (var field in inputObjectType.Fields)
                    {
                        if (field.Type.IsNonNullType()
                            && field.DefaultValue is null
                            && field.Directives.Any(d => d.Definition is RequiresOptInDirectiveType))
                        {
                            errors.Add(RequiresOptInOnRequiredInputField(
                                inputObjectType,
                                field));
                        }
                    }

                    break;

                case IComplexTypeDefinition complexType:
                    foreach (var field in complexType.Fields)
                    {
                        foreach (var argument in field.Arguments)
                        {
                            if (argument.Type.IsNonNullType()
                                && argument.DefaultValue is null
                                && argument.Directives.Any(d => d.Definition is RequiresOptInDirectiveType))
                            {
                                errors.Add(RequiresOptInOnRequiredArgument(
                                    complexType,
                                    field,
                                    argument));
                            }
                        }
                    }

                    break;
            }
        }
    }
}
