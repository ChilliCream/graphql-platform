namespace HotChocolate.Data.Neo4J.Language
{
    public abstract class FunctionDefinition
        : Expression
        , IFunctionDefinition
    {
        private readonly string _implementationName;

        public FunctionDefinition(string implementationName)
        {
            _implementationName = implementationName;
        }

        public override ClauseKind Kind => ClauseKind.FunctionDefinition;

        public string GetImplementationName() => _implementationName;

        public bool IsAggregate() => false;
    }
}
