using HotChocolate.Execution;
using HotChocolate.Fusion.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Configuration;

internal sealed class FusionGatewaySetup
{
    public List<Action<FusionRequestOptions>> RequestOptionsModifiers { get; } = [];

    public List<Action<IServiceProvider, IServiceCollection>> SchemaServiceModifiers { get; } = [];

    public List<Action<IList<RequestMiddlewareConfiguration>>> PipelineModifiers { get; } = [];
}
