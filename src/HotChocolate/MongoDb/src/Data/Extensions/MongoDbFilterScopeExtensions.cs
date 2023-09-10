namespace HotChocolate.Data.MongoDb.Filters;

public static class MongoDbFilterScopeExtensions
{
    public static string GetPath(this MongoDbFilterScope scope) =>
        string.Join(".", scope.Path.Reverse());

    public static MongoDbFilterDefinition CreateQuery(this MongoDbFilterScope scope)
    {
        if (scope.Level.Peek().Count == 0)
        {
            return MongoDbFilterDefinition.Empty;
        }

        return new AndFilterDefinition(scope.Level.Peek().ToArray());
    }
}
