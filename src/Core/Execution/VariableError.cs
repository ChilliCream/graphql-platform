namespace HotChocolate
{
    public class VariableError
        : QueryError
    {
        public VariableError(string message, string variableName)
            : base(message)
        {
            VariableName = variableName;
        }

        public string VariableName { get; }
    }
}
