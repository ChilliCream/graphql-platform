namespace HotChocolate.Data.Neo4J.Language
{
    public class ConditionBuilder
    {
        private static Condition _condition;

        public static void Where(Condition condition)
        {
            _condition = condition;
        }

        public static void And(Condition condition)
        {
            _condition = _condition.And(condition);
        }

        public Condition BuildCondition()
        {
            return _condition;
        }
    }
}
