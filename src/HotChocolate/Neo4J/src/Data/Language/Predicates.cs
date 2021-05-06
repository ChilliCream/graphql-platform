namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    /// Factory methods for creating predicates
    /// </summary>
    public class Predicates
    {
        /// <summary>
        /// https://neo4j.com/docs/cypher-manual/current/functions/predicate/#functions-exists
        /// </summary>
        /// <param name="property"></param>
        /// <returns></returns>
        public static Condition Exists(Property property) =>
            new BooleanFunctionCondition(
                FunctionInvocation.Create(BuiltInFunctions.Predicates.Exists, property));

        /// <summary>
        /// https://neo4j.com/docs/cypher-manual/current/functions/predicate/#functions-exists
        /// </summary>
        /// <param name="pattern"></param>
        /// <returns></returns>
        public static Condition Exists(IRelationshipPattern pattern) =>
            new BooleanFunctionCondition(
                FunctionInvocation.Create(BuiltInFunctions.Predicates.Exists, pattern));

        /// <summary>
        /// https://neo4j.com/docs/cypher-manual/current/functions/predicate/#functions-all
        /// </summary>
        /// <param name="variable"></param>
        /// <returns></returns>
        public static IOngoingListBasedPredicateFunction All(string variable) =>
            All(SymbolicName.Of(variable));

        /// <summary>
        /// https://neo4j.com/docs/cypher-manual/current/functions/predicate/#functions-all
        /// </summary>
        /// <param name="variable"></param>
        /// <returns></returns>
        public static IOngoingListBasedPredicateFunction All(SymbolicName variable) =>
            new Builder(BuiltInFunctions.Predicates.All, variable);

        /// <summary>
        ///
        /// </summary>
        /// <param name="variable"></param>
        /// <returns></returns>
        public static IOngoingListBasedPredicateFunction Any(string variable) =>
            Any(SymbolicName.Of(variable));

        /// <summary>
        ///
        /// </summary>
        /// <param name="variable"></param>
        /// <returns></returns>
        public static IOngoingListBasedPredicateFunction Any(SymbolicName variable) =>
            new Builder(BuiltInFunctions.Predicates.Any, variable);

        /// <summary>
        ///
        /// </summary>
        /// <param name="variable"></param>
        /// <returns></returns>
        public static IOngoingListBasedPredicateFunction None(string variable) =>
            Single(SymbolicName.Of(variable));

        /// <summary>
        ///
        /// </summary>
        /// <param name="variable"></param>
        /// <returns></returns>
        public static IOngoingListBasedPredicateFunction None(SymbolicName variable) =>
            new Builder(BuiltInFunctions.Predicates.None, variable);

        /// <summary>
        ///
        /// </summary>
        /// <param name="variable"></param>
        /// <returns></returns>
        public static IOngoingListBasedPredicateFunction Single(string variable) =>
            Single(SymbolicName.Of(variable));

        /// <summary>
        /// https://neo4j.com/docs/cypher-manual/current/functions/predicate/#functions-single
        /// </summary>
        /// <param name="variable">The variable referring to elements of a list</param>
        /// <returns>A builder for the single() predicate function</returns>
        public static IOngoingListBasedPredicateFunction Single(SymbolicName variable) =>
            new Builder(BuiltInFunctions.Predicates.Single, variable);

        /// <summary>
        ///
        /// </summary>
        private class Builder
            : IOngoingListBasedPredicateFunction
                , IOngoingListBasedPredicateFunctionWithList
        {

            private readonly BuiltInFunctions.Predicates _predicate;
            private readonly SymbolicName _name;
            private Expression _listExpression;

            public Builder(BuiltInFunctions.Predicates predicate, SymbolicName name) {

                Ensure.IsNotNull(predicate, "The predicate is required");
                Ensure.IsNotNull(name, "The name is required");
                _predicate = predicate;
                _name = name;
            }

            public IOngoingListBasedPredicateFunctionWithList In(Expression list)
            {
                Ensure.IsNotNull(list, "The list expression is required");
                _listExpression = list;
                return this;
            }

            public Condition Where(Condition condition)
            {
                Ensure.IsNotNull(condition, "The condition is required");
                return new BooleanFunctionCondition(
                    FunctionInvocation.Create(_predicate, new ListPredicate(_name, _listExpression, new Where(condition))));
            }
        }
    }

    /// <summary>
    ///
    /// </summary>
    public interface IOngoingListBasedPredicateFunction
    {
        IOngoingListBasedPredicateFunctionWithList In(Expression list);
    }

    /// <summary>
    ///
    /// </summary>
    public interface IOngoingListBasedPredicateFunctionWithList
    {
        Condition Where(Condition condition);
    }
}
