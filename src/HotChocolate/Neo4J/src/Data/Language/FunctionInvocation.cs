using System.Linq.Expressions;

namespace HotChocolate.Data.Neo4J.Language
{
    public class FunctionInvocation : Expression
    {
        public override ClauseKind Kind { get; } = ClauseKind.FunctionInvocation;

        private readonly string _functionName;
        private readonly ExpressionList _arguments;

        public override void Visit(CypherVisitor cypherVisitor)
        {
            cypherVisitor.Enter(this);
            _arguments.Visit(cypherVisitor);
            cypherVisitor.Leave(this);
        }
    }
}
