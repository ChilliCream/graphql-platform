using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.AspNetCore.Mvc;

namespace HotChocolate.AspNetCore.Tests.Utilities;

[ExtendObjectType("Query")]
public class QueryExtension
{
    public long Time(Schema schema)
        => schema.CreatedAt.Ticks;

    public bool Evict([FromServices] IRequestExecutorResolver executorResolver, ISchema schema)
    {
        executorResolver.EvictRequestExecutor(schema.Name);
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
