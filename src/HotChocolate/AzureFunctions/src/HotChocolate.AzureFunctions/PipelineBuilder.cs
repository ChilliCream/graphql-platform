using Microsoft.AspNetCore.Http;

namespace HotChocolate.AzureFunctions;

internal sealed class PipelineBuilder
{
    private readonly List<Func<RequestDelegate, RequestDelegate>> _components = [];

    public PipelineBuilder Use(Func<RequestDelegate, RequestDelegate> middleware)
    {
        ArgumentNullException.ThrowIfNull(middleware);

        _components.Add(middleware);
        return this;
    }

    public RequestDelegate Compile(IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(services);

        if (_components.Count == 0)
        {
            throw new InvalidOperationException(
                "There must be at least one component in order to build a pipeline.");
        }

        RequestDelegate next = context =>
        {
            context.Response.StatusCode = 404;
            return Task.CompletedTask;
        };

        for (var i = _components.Count - 1; i >= 0; i--)
        {
            next = _components[i].Invoke(next);
        }

        return next;
    }
}
