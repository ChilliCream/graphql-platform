namespace HotChocolate.Data.Neo4J.Language
{
    public abstract class FunctionDefinition
        : Expression
        , IFunctionDefinition
    {
        public FunctionDefinition(string implementationName)
        {
            ImplementationName = implementationName;
        }

        public override ClauseKind Kind => ClauseKind.FunctionDefinition;

        public string ImplementationName { get; }

        public bool IsAggregate => false;
    }
}
