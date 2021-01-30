namespace HotChocolate.Data.Neo4J.Language
{
    public abstract class Condition : Expression
    {
        public Condition And(Condition condition) =>
            CompoundCondition.Create(this, Operator.And, condition);
        public static Condition And(Condition condition1, Condition condition2) =>
            CompoundCondition.Create(condition1, Operator.And, condition2);

        public Condition Or(Condition condition) =>
            CompoundCondition.Create(this, Operator.Or, condition);

        public static Condition Or(Condition condition1, Condition condition2) =>
            CompoundCondition.Create(condition1, Operator.Or, condition2);

        public Condition XOr(Condition condition) =>
            CompoundCondition.Create(this, Operator.XOr, condition);

        public static Condition XOr(Condition condition1, Condition condition2) =>
            CompoundCondition.Create(condition1, Operator.XOr, condition2);

        public Condition Not() => Comparison.Create(Operator.Not, this);
    }
}
