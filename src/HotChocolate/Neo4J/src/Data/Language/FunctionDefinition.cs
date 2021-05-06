namespace HotChocolate.Data.Neo4J.Language
{
    public abstract class FunctionDefinition : Expression, IFunctionDefinition
    {
        public override ClauseKind Kind => ClauseKind.FunctionDefinition;

        private readonly string _implementationName;

        public FunctionDefinition(string implementationName)
        {
            _implementationName = implementationName;
        }

        public string GetImplementationName() => _implementationName;

        public bool IsAggregate() => false;
    }
}
