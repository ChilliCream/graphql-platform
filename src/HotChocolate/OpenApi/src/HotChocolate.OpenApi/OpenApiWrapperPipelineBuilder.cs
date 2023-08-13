namespace HotChocolate.OpenApi;

internal sealed class OpenApiWrapperPipelineBuilder
{
    private readonly List<OpenApiWrapperMiddleware> _pipeline = new();

    private OpenApiWrapperPipelineBuilder()
    {
    }

    public static OpenApiWrapperPipelineBuilder New() => new();

    private OpenApiWrapperPipelineBuilder Use(OpenApiWrapperMiddleware middleware)
    {
        _pipeline.Add(middleware);
        return this;
    }

    public OpenApiWrapperPipelineBuilder Use<TMiddleware>()
        where TMiddleware : IOpenApiWrapperMiddleware, new()
    {
        Use(next =>
        {
            var middleware = new TMiddleware();
            return context => middleware.Invoke(context, next);
        });
        return this;
    }

    public OpenApiWrapperDelegate Build()
    {
        OpenApiWrapperDelegate next = _ => { };
        for (var i = _pipeline.Count - 1; i >= 0; i--)
        {
            next = _pipeline[i].Invoke(next);
        }
        return next;
    }
}
