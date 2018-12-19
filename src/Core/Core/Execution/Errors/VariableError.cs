using System;

namespace HotChocolate.Execution
{
    [Obsolete(
        "Use QueryError.CreateVariableError(message, variableName).")]
    public class VariableError
        : QueryError
    {
        public VariableError(string message, string variableName)
            : base(message, new ErrorProperty("variableName", variableName))
        {
        }
    }
}
