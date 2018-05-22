using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Types;

namespace HotChocolate
{
    public class SchemaException
        : Exception
    {
        public SchemaException(IEnumerable<SchemaError> errors)
        {
            if (errors == null)
            {
                throw new ArgumentNullException(nameof(errors));
            }

            Errors = errors.ToArray();
        }

        public IReadOnlyCollection<SchemaError> Errors { get; }
    }
}
