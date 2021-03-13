using System.Linq.Expressions;

namespace HotChocolate.Data.Neo4J.Language
{
    public class FunctionInvocation : Expression
    {
        public override ClauseKind Kind { get; } = ClauseKind.FunctionInvocation;

        private readonly string _functionName;
        private readonly ExpressionList _arguments;

        public FunctionInvocation(string functionName, params Expression[] arguments)
        {
            _functionName = functionName;
            _arguments = new ExpressionList(arguments);
        }

        public static FunctionInvocation Create(FunctionDefinition definition, params Expression[] expressions) {

            string message = "The expression for " + definition.GetImplementationName() + "() is required.";

            //Ensure.IsNotEmpty(expressions, message);
            Ensure.IsNotNull(expressions[0], message);

            return new FunctionInvocation(definition.GetImplementationName(), expressions);
        }

        // public static FunctionInvocation Create(FunctionDefinition definition, ExpressionList arguments) {
        //
        //     Ensure.IsNotNull(arguments, definition.GetImplementationName() + "() requires at least one argument.");
        //
        //     return new FunctionInvocation(definition.GetImplementationName(), arguments);
        // }

        public override void Visit(CypherVisitor cypherVisitor)
        {
            cypherVisitor.Enter(this);
            _arguments.Visit(cypherVisitor);
            cypherVisitor.Leave(this);
        }
    }
}
