using HotChocolate.Data.Filters;

namespace HotChocolate.Data.MongoDb.Filters;

public static class MongoFilterVisitorContextExtensions
{
    /// <summary>
    /// Reads the current scope from the context
    /// </summary>
    /// <param name="context">The context</param>
    /// <returns>The current scope</returns>
    public static MongoDbFilterScope GetMongoFilterScope(this MongoDbFilterVisitorContext context)
        => (MongoDbFilterScope)context.GetScope();

    /// <summary>
    /// Tries to build the query based on the items that are stored on the scope
    /// </summary>
    /// <param name="context">the context</param>
    /// <returns>True in case the query has been build successfully, otherwise false</returns>
    public static MongoDbFilterDefinition CreateQuery(this MongoDbFilterVisitorContext context)
        => context.GetMongoFilterScope().CreateQuery();
}
