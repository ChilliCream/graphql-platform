using HotChocolate.Types.Descriptors;

namespace HotChocolate.Configuration.Validation;

internal static class SchemaValidator
{
    private static readonly ISchemaValidationRule[] s_rules =
    [
        new ObjectTypeValidationRule(),
        new InterfaceTypeValidationRule(),
        new InputObjectTypeValidationRule(),
        new DirectiveValidationRule(),
        new InterfaceHasAtLeastOneImplementationRule(),
        new IsSelectedPatternValidation(),
        new EnsureFieldResultsDeclareErrorsRule(),
        new RequiresOptInValidationRule()
    ];

    public static IReadOnlyList<ISchemaError> Validate(
        IDescriptorContext context,
        ISchemaDefinition schema)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(schema);

        var errors = new List<ISchemaError>();

        foreach (var rule in s_rules)
        {
            rule.Validate(context, schema, errors);
        }

        return errors;
    }
}
