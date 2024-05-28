using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CookieCrumble;

public static class CommonTestExtensions
{
    public static ValueTask<IRequestExecutor> CreateExceptionExecutor(
        this IRequestExecutorBuilder builder)
    {
        return builder.UseRequest(
            next => async context =>
            {
                await next(context);
                if (context.ContextData.TryGetValue("ex", out var queryString))
                {
                    context.Result =
                        OperationResultBuilder
                            .FromResult(context.Result!.ExpectQueryResult())
                            .SetContextData("ex", queryString)
                            .Build();
                }
            })
            .UseDefaultPipeline()
            .Services
            .BuildServiceProvider()
            .GetRequiredService<IRequestExecutorResolver>()
            .GetRequestExecutorAsync();
    }
}
