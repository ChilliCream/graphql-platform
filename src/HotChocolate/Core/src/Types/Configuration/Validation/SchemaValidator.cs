using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Types;

namespace HotChocolate.Configuration.Validation
{
    internal static class SchemaValidator
    {
        private static ISchemaValidationRule[] _rules =
            new ISchemaValidationRule[]
            {
                new ObjectTypeValidationRule(),
                new InterfaceTypeValidationRule(),
                new InterfaceHasAtLeastOneImplementationRule()
            };

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

            var types = typeSystemObjects.ToList();
            var errors = new List<ISchemaError>();

            foreach (ISchemaValidationRule rule in _rules)
            {
                rule.Validate(types, options, errors);
            }

            return errors;
        }
    }
}
