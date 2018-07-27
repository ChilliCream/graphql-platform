using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace HotChocolate
{
    public class SchemaException
        : Exception
    {
        public SchemaException(params SchemaError[] errors)
            : base(CreateErrorMessage(errors))
        {
            Errors = errors;
            PrintErrors(Errors);
        }

        public SchemaException(IEnumerable<SchemaError> errors)
            : base(CreateErrorMessage(errors.ToArray()))
        {
            Errors = errors.ToArray();
            PrintErrors(Errors);
        }

        public IReadOnlyCollection<SchemaError> Errors { get; }

        private static string CreateErrorMessage(
            IReadOnlyCollection<SchemaError> errors)
        {
            if (errors == null || errors.Count == 0)
            {
                return "Unexpected schema exception occured.";
            }
            else if (errors.Count == 1)
            {
                return errors.First().Message;
            }
            else
            {
                return "Multiple schema errors occured. " +
                    "For more details check `Errors` property.";
            }
        }

        private static void PrintErrors(IReadOnlyCollection<SchemaError> errors)
        {
            Debug.WriteLine("Schema Errors:");
            foreach (SchemaError error in errors)
            {
                Debug.WriteLine(
                    $"Type: {error.Type.Name} - Message: {error.Message}");
            }
        }
    }
}
