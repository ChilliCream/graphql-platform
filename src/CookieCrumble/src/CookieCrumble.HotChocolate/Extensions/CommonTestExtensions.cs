using HotChocolate;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;

namespace CookieCrumble.HotChocolate;

public static class CommonTestExtensions
{
    public static ValueTask<IRequestExecutor> CreateExceptionExecutor(
        this IRequestExecutorBuilder builder)
    {
        return builder.UseRequest(
            (_, next) => async context =>
            {
                await next(context);
                if (context.ContextData.TryGetValue("ex", out var queryString))
                {
                    context.Result!.ContextData = context.Result.ContextData.SetItem("ex", queryString);
                }
            })
            .UseDefaultPipeline()
            .Services
            .BuildServiceProvider()
            .GetRequiredService<IRequestExecutorProvider>()
            .GetExecutorAsync();
    }

    public static Operation CreateOperation()
    {
        var schema =
            SchemaBuilder.New()
                .AddDocumentFromString(
                    """
                    type Query {
                      first: String
                    }
                    """)
                .Use(_ => _)
                .Create();

        return OperationCompiler.Compile(
            "abc",
            Utf8GraphQLParser.Parse(
                """
                {
                  first
                }
                """),
            schema);
    }
}
