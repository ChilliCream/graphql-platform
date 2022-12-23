namespace HotChocolate.Data.Neo4J.Testing;

public abstract class Neo4JFixtureBase
{
    private readonly string[] _resetDatabase = { "MATCH (a) -[r] -> () DELETE a, r", "MATCH (a) DELETE a" };

    protected async Task ResetDatabase(Neo4JDatabase database, string cypher)
    {
        var session = database.GetAsyncSession();
        foreach (var action in _resetDatabase)
        {
            var resetCursor = await session.RunAsync(action);
            await resetCursor.ConsumeAsync();
        }

        var cursor = await session.RunAsync(cypher);
        await cursor.ConsumeAsync();
    }
}
