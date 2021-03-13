namespace HotChocolate.Data.Neo4J.Language
{
    public class Predicates
    {
        public static Condition Exists(Property property) {

            return new BooleanFunctionCondition(
                FunctionInvocation.Create(BuiltInFunctions.Predicates.Exists, property));
        }

        // public static Condition Exists(IRelationshipPattern pattern) {
        //
        //     return new BooleanFunctionCondition(FunctionInvocation.Create(BuiltInFunctions.Predicates.Exists, pattern));
        // }

        public static IOngoingListBasedPredicateFunction All(string variable) {

            return All(SymbolicName.Of(variable));
        }

        public static IOngoingListBasedPredicateFunction All(SymbolicName variable) {

            return new Builder(BuiltInFunctions.Predicates.All, variable);
        }

        public static IOngoingListBasedPredicateFunction Any(string variable) {

            return Any(SymbolicName.Of(variable));
        }

        public static IOngoingListBasedPredicateFunction Any(SymbolicName variable) {

            return new Builder(BuiltInFunctions.Predicates.Any, variable);
        }

        private class Builder
            : IOngoingListBasedPredicateFunction
                , IOngoingListBasedPredicateFunctionWithList
        {

            private readonly BuiltInFunctions.Predicates _predicate;
            private readonly SymbolicName _name;
            private Expression listExpression;

            public Builder(BuiltInFunctions.Predicates predicate, SymbolicName name) {

                Ensure.IsNotNull(predicate, "The predicate is required");
                Ensure.IsNotNull(name, "The name is required");
                _predicate = predicate;
                _name = name;
            }

            public IOngoingListBasedPredicateFunctionWithList In(Expression list)
            {
                throw new System.NotImplementedException();
            }

            public Condition Where(Condition condition)
            {
                throw new System.NotImplementedException();
            }
        }
    }



    public interface IOngoingListBasedPredicateFunction
    {
        IOngoingListBasedPredicateFunctionWithList In(Expression list);
    }

    public interface IOngoingListBasedPredicateFunctionWithList
    {
        Condition Where(Condition condition);
    }
}
