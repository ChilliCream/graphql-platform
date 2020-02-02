using System;
using HotChocolate.Language;

namespace HotChocolate.Execution
{
    [Obsolete(
        "Use QueryError.CreateFieldError(message, path, fieldSelection)." +
        "This type will be removed with version 1.0.0.")]
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
