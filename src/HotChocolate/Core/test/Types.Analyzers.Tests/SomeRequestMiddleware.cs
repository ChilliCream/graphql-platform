using System.Collections.Generic;
using System.Threading.Tasks;
using HotChocolate.Execution;

namespace HotChocolate.Types;

public class SomeRequestMiddleware(RequestDelegate next)
{
    public async ValueTask InvokeAsync(IRequestContext context)
    {
        await next(context);

        context.Result =
            QueryResultBuilder.New()
                .SetData(new Dictionary<string, object?> { { "hello", true } })
                .Create();
    }
}