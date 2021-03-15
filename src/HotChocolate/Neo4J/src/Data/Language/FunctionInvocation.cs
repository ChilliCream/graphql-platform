namespace HotChocolate.Data.Neo4J.Language
{
    public class FunctionInvocation : Expression
    {
        public override ClauseKind Kind { get; } = ClauseKind.FunctionInvocation;

        private readonly string _functionName;
        private readonly TypedSubtree<Expression> _arguments;

        public FunctionInvocation(string functionName, params Expression[] arguments)
        {
            _functionName = functionName;
            _arguments = new ExpressionList(arguments);
        }

        private FunctionInvocation(string functionName, TypedSubtree<Expression> pattern) {

            _functionName = functionName;
            _arguments = pattern;
        }

        public static FunctionInvocation Create(FunctionDefinition definition, params Expression[] expressions) {

            var message = "The expression for " + definition.GetImplementationName() + "() is required.";

            Ensure.IsNotEmpty(expressions, message);
            Ensure.IsNotNull(expressions[0], message);

            return new FunctionInvocation(definition.GetImplementationName(), expressions);
        }

        public static FunctionInvocation Create(FunctionDefinition definition, ExpressionList arguments) {

            Ensure.IsNotNull(arguments, definition.GetImplementationName() + "() requires at least one argument.");

            return new FunctionInvocation(definition.GetImplementationName(), arguments);
        }

        // static FunctionInvocation Create(FunctionDefinition definition, IPatternElement pattern) {
        //
        //     Ensure.IsNotNull(pattern, "The pattern for " + definition.GetImplementationName() + "() is required.");
        //
        //     return new FunctionInvocation(definition.GetImplementationName(),  new ExpressionList(pattern));
        // }

        public string GetFunctionName() => _functionName;

        public override void Visit(CypherVisitor cypherVisitor)
        {
            cypherVisitor.Enter(this);
            _arguments.Visit(cypherVisitor);
            cypherVisitor.Leave(this);
        }
    }
}
