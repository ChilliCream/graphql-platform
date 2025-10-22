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
                        if (field.Type.IsNonNullType() && field.DefaultValue is null)
                        {
                            var requiresOptInDirectives = field.Directives
                                .Where(d => d.Definition is RequiresOptInDirectiveType);

                            foreach (var _ in requiresOptInDirectives)
                            {
                                errors.Add(RequiresOptInOnRequiredInputField(
                                    inputObjectType,
                                    field));
                            }
                        }
                    }

                    break;

                case IObjectTypeDefinition objectType:
                    foreach (var field in objectType.Fields)
                    {
                        foreach (var argument in field.Arguments)
                        {
                            if (argument.Type.IsNonNullType() && argument.DefaultValue is null)
                            {
                                var requiresOptInDirectives = argument.Directives
                                    .Where(d => d.Definition is RequiresOptInDirectiveType);

                                foreach (var _ in requiresOptInDirectives)
                                {
                                    errors.Add(RequiresOptInOnRequiredArgument(
                                        objectType,
                                        field,
                                        argument));
                                }
                            }
                        }
                    }

                    break;
            }
        }
    }
}
