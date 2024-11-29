using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CookieCrumble.HotChocolate;

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
                            .FromResult(context.Result!.ExpectOperationResult())
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
