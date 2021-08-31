namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    ///
    /// </summary>
    public interface IOngoingListBasedPredicateFunction
    {
        IOngoingListBasedPredicateFunctionWithList In(Expression list);
    }
}
