using System;
using System.Collections.Generic;
using HotChocolate.Language;

namespace HotChocolate.Execution
{
    [Obsolete(
        "Use QueryError.CreateFieldError(message, path, fieldSelection).")]
    public class FieldError
        : QueryError
    {
        public FieldError(string message, FieldNode fieldSelection)
            : base(message, ConvertLocation(fieldSelection.Location),
                new ErrorProperty("fieldName", fieldSelection.Name.Value))
        {
        }
    }
}
