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
                new InterfaceHasAtLeastOneImplementationRule(),
                new InetrfaceFieldsAreImplementedRule()
            };

        public static IReadOnlyCollection<ISchemaError> Validate(
            IEnumerable<ITypeSystemObject> typeSystemObjects)
        {
            if (typeSystemObjects == null)
            {
                throw new ArgumentNullException(nameof(typeSystemObjects));
            }

            var types = typeSystemObjects.ToList();
            var errors = new List<ISchemaError>();

            foreach (ISchemaValidationRule rule in _rules)
            {
                errors.AddRange(rule.Validate(types));
            }
            return errors;
        }
    }
}
