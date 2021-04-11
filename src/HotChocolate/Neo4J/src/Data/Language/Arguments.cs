namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    /// Specialized list of expressions that represent the arguments of a procedure call.
    /// </summary>
    public class Arguments : TypedSubtree<Expression>, ITypedSubtree
    {
        public override ClauseKind Kind => ClauseKind.Arguments;
        public Arguments(params Expression[] children) : base(children) { }

        protected override IVisitable PrepareVisit(Expression child) =>
            Expressions.NameOrExpression(child);
    }
}
