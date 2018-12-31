using System;

namespace HotChocolate.Execution
{
    [Obsolete(
        "Use QueryError.CreateVariableError(message, variableName). " +
        "This type will be removed with version 1.0.0.")]
    public class VariableError
        : QueryError
    {
        public VariableError(string message, string variableName)
            : base(message, new ErrorProperty("variableName", variableName))
        {
        }
    }
}
