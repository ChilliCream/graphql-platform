using System;
using System.Collections.Generic;
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
        }

        public SchemaException(IEnumerable<SchemaError> errors)
            : base(CreateErrorMessage(errors.ToArray()))
        {
            Errors = errors.ToArray();
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
    }
}
