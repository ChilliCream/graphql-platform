using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Types;

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
    ];

    public static IReadOnlyCollection<ISchemaError> Validate(
        IEnumerable<ITypeSystemObject> typeSystemObjects,
        IReadOnlySchemaOptions options)
    {
        if (typeSystemObjects is null)
        {
            throw new ArgumentNullException(nameof(typeSystemObjects));
        }

        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        var types = typeSystemObjects.ToArray();
        var errors = new List<ISchemaError>();

        foreach (var rule in _rules)
        {
            rule.Validate(types, options, errors);
        }

        return errors;
    }
}
