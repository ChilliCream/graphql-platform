namespace HotChocolate.Data.Neo4J.Language
{
    public abstract class Condition : Expression
    {
        public Condition And(Condition condition) =>
            CompoundCondition.Create(this, Operator.And, condition);

        public Condition Or(Condition condition) =>
            CompoundCondition.Create(this, Operator.Or, condition);

        public Condition XOr(Condition condition) =>
            CompoundCondition.Create(this, Operator.XOr, condition);

        public Condition Not() => Comparison.Create(Operator.Not, this);
    }
}
