using HotChocolate.Types.Descriptors;

namespace HotChocolate.Configuration.Validation;

internal static class SchemaValidator
{
    private static readonly ISchemaValidationRule[] _rules =
    [
        new ObjectTypeValidationRule(),
        new InterfaceTypeValidationRule(),
        new InputObjectTypeValidationRule(),
        new DirectiveValidationRule(),
        new InterfaceHasAtLeastOneImplementationRule(),
        new IsSelectedPatternValidation(),
        new EnsureFieldResultsDeclareErrorsRule()
    ];

    public static IReadOnlyList<ISchemaError> Validate(
        IDescriptorContext context,
        ISchema schema)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (schema == null)
        {
            throw new ArgumentNullException(nameof(schema));
        }

        var errors = new List<ISchemaError>();

        foreach (var rule in _rules)
        {
            rule.Validate(context, schema, errors);
        }

        return errors;
    }
}
