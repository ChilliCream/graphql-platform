namespace HotChocolate.Data.Neo4J.Language
{
    public class FunctionInvocation : Expression
    {
        public FunctionInvocation(string functionName, params Expression[] arguments)
        {
            FunctionName = functionName;
            Arguments = new ExpressionList(arguments);
        }

        public FunctionInvocation(string functionName, Pattern pattern)
        {
            FunctionName = functionName;
            PatternArguments = pattern;
        }

        private FunctionInvocation(string functionName, TypedSubtree<Expression> pattern)
        {
            FunctionName = functionName;
            Arguments = pattern;
        }

        public override ClauseKind Kind => ClauseKind.FunctionInvocation;

        public string FunctionName { get; }

        public TypedSubtree<Expression>? Arguments { get; }

        public TypedSubtree<IPatternElement>? PatternArguments { get; }

        public override void Visit(CypherVisitor cypherVisitor)
        {
            cypherVisitor.Enter(this);
            Arguments?.Visit(cypherVisitor);
            PatternArguments?.Visit(cypherVisitor);
            cypherVisitor.Leave(this);
        }

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
    }
}
