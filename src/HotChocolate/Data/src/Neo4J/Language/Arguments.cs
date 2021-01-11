namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    /// Specialized list of expressions that represent the arguments on a procedure call.
    /// </summary>
    public class Arguments : TypedSubtree<Expression, Arguments>
    {
        public override ClauseKind Kind => ClauseKind.Default;
        public Arguments(params Expression[] children) : base(children) { }
        protected static new Visitable PrepareVisit(Expression child) =>
            Expressions.NameOrExpression(child);
    }
}
