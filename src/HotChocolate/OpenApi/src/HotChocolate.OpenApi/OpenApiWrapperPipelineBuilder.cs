namespace HotChocolate.OpenApi;

internal sealed class OpenApiWrapperPipelineBuilder
{
    private readonly List<OpenApiWrapperMiddleware> _pipeline = [];

    private OpenApiWrapperPipelineBuilder()
    {
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

    public static OpenApiWrapperPipelineBuilder New() => new();

    public OpenApiWrapperPipelineBuilder Use<TMiddleware>()
        where TMiddleware : IOpenApiWrapperMiddleware, new()
    {
        _pipeline.Add(next =>
        {
            var middleware = new TMiddleware();
            return context => middleware.Invoke(context, next);
        });
        
        return this;
    }
}
