using System;
using HotChocolate.Language;

namespace HotChocolate.Execution
{
    [Obsolete(
        "Use QueryError.CreateArgumentError(message, path, argument).")]
    public class ArgumentError
        : QueryError
    {
        public ArgumentError(string message, string argumentName,
            FieldNode fieldSelection)
            : base(message, ConvertLocation(fieldSelection.Location),
                new ErrorProperty("fieldName", fieldSelection.Name.Value),
                new ErrorProperty("argumentName", argumentName))
        {
        }
    }
}
