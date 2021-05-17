namespace HotChocolate.Data.Neo4J.Language
{
    public class FunctionInvocation : Expression
    {
        private readonly TypedSubtree<Expression>? _arguments;
        private readonly TypedSubtree<IPatternElement>? _patternArguments;

        public FunctionInvocation(string functionName, params Expression[] arguments)
        {
            FunctionName = functionName;
            _arguments = new ExpressionList(arguments);
        }

        public FunctionInvocation(string functionName, Pattern pattern)
        {
            FunctionName = functionName;
            _patternArguments = pattern;
        }

        private FunctionInvocation(string functionName, TypedSubtree<Expression> pattern)
        {
            FunctionName = functionName;
            _arguments = pattern;
        }

        public override ClauseKind Kind => ClauseKind.FunctionInvocation;

        public string FunctionName { get; }

        public static FunctionInvocation Create(
            FunctionDefinition definition,
            params Expression[] expressions)
        {
            var message = $"The expression for {definition.ImplementationName}() is required.";

            Ensure.IsNotEmpty(expressions, message);
            Ensure.IsNotNull(expressions[0], message);

            return new FunctionInvocation(definition.ImplementationName, expressions);
        }

        public static FunctionInvocation Create(
            FunctionDefinition definition,
            ExpressionList arguments)
        {
            Ensure.IsNotNull(
                arguments,
                definition.ImplementationName + "() requires at least one argument.");

            return new FunctionInvocation(definition.ImplementationName, arguments);
        }

        public static FunctionInvocation Create(
            FunctionDefinition definition,
            IPatternElement pattern)
        {
            Ensure.IsNotNull(
                pattern,
                $"The pattern for {definition.ImplementationName}() is required.");

            return new FunctionInvocation(definition.ImplementationName, new Pattern(pattern));
        }

        public override void Visit(CypherVisitor cypherVisitor)
        {
            cypherVisitor.Enter(this);
            _arguments?.Visit(cypherVisitor);
            _patternArguments?.Visit(cypherVisitor);
            cypherVisitor.Leave(this);
        }
    }
}
