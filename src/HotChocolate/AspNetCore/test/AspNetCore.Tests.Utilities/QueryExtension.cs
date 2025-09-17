using HotChocolate.Execution;
using HotChocolate.Types;

namespace HotChocolate.AspNetCore.Tests.Utilities;

[ExtendObjectType("Query")]
public class QueryExtension
{
    public long Time(Schema schema)
        => schema.CreatedAt.Ticks;

    public bool Evict(IRequestExecutorProvider executorResolver, ISchemaDefinition schema)
    {
        ((RequestExecutorManager)executorResolver).EvictExecutor(schema.Name);
        return true;
    }

    public async Task<bool> Wait(int m, CancellationToken ct)
    {
        await Task.Delay(m, ct);
        return true;
    }

    [GraphQLDeprecated("use something else")]
    public string SomeDeprecatedField(
        [GraphQLDeprecated("use something else")]
        string deprecatedArg = "foo")
        => "foo";
}
