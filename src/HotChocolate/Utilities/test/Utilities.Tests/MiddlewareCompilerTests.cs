using System.Linq.Expressions;
using Xunit;

namespace HotChocolate.Utilities;

public class MiddlewareCompilerTests
{
    [Fact]
    public void CompileFactory()
    {
        // arrange
        // act
        var factory =
            MiddlewareCompiler<CustomClassMiddleware>
                .CompileFactory<IServiceProvider, CustomDelegate>(
                    (services, _) =>
                        new List<IParameterHandler>
                        {
                            new TypeParameterHandler(typeof(string), Expression.Constant("abc")),
                            new ServiceParameterHandler(services),
                        });

        // assert
        var middleware = factory.Invoke(EmptyServiceProvider.Instance, _ => default);
        Assert.Equal("abc", middleware.Some);
    }

    [Fact]
    public void CompileDelegate()
    {
        // arrange
        var factory =
            MiddlewareCompiler<CustomClassMiddleware>
                .CompileFactory<IServiceProvider, CustomDelegate>(
                    (services, _) =>
                        new List<IParameterHandler>
                        {
                            new TypeParameterHandler(typeof(string), Expression.Constant("abc")),
                            new ServiceParameterHandler(services),
                        });

        var middleware = factory.Invoke(EmptyServiceProvider.Instance, _ => default);

        // act
        var pipeline =
            MiddlewareCompiler<CustomClassMiddleware>.CompileDelegate<CustomContext>(
                (_, _) =>
                    new List<IParameterHandler>
                    {
                        new TypeParameterHandler(typeof(string), Expression.Constant("def")),
                    });

        // assert
        var context = new CustomContext(EmptyServiceProvider.Instance);
        pipeline.Invoke(context, middleware);
        Assert.Equal("abcdef", context.Result);
    }

    public class CustomClassMiddleware(CustomDelegate next, string some)
    {
        public string Some { get; } = some;

        public async Task InvokeAsync(CustomContext context, string some)
        {
            context.Result = Some + some;
            await next(context);
        }
    }

    public class CustomContext(IServiceProvider services)
    {
        public IServiceProvider Services { get; } = services;

        public string? Result { get; set; }
    }

    public delegate ValueTask CustomDelegate(CustomContext context);

    public delegate CustomContext CustomMiddleware(CustomDelegate next);
}
