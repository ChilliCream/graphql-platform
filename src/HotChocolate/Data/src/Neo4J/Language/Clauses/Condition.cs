namespace HotChocolate.Data.Neo4J.Language
{
    public abstract class Condition : Expression
    {
        public Condition And(Condition condition) =>
            CompoundCondition.Create(this, Operator.And, condition);

        public Condition And(RelationshipPattern pattern) =>
            CompoundCondition.Create(this, Operator.And, new RelationshipPatternCondition(pattern));
        public Condition Or(Condition condition) =>
            CompoundCondition.Create(this, Operator.Or, condition);

        public Condition Or(RelationshipPattern pattern) =>
            CompoundCondition.Create(this, Operator.Or, new RelationshipPatternCondition(pattern));

        public Condition XOr(Condition condition) =>
            CompoundCondition.Create(this, Operator.XOr, condition);

        public Condition XOr(RelationshipPattern pattern) =>
            CompoundCondition.Create(this, Operator.XOr, new RelationshipPatternCondition(pattern));

        public Condition Not() => Comparison.Create(Operator.Not, this);
    }
}