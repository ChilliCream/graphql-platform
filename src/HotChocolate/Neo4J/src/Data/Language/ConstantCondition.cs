namespace HotChocolate.Data.Neo4J.Language
{
    public class ConstantCondition : ExpressionCondition
    {
        public static readonly ConstantCondition True = new (BooleanLiteral.True);
        public static readonly ConstantCondition False = new (BooleanLiteral.False);

        private ConstantCondition(Expression value) : base(value) { }
    }
}
