using HotChocolate.Execution;
using HotChocolate.Features;
using HotChocolate.Fusion.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Configuration;

internal sealed class FusionGatewaySetup
{
    public Func<IServiceProvider, IFusionSchemaDocumentProvider>? DocumentProvider { get; set; }

    public List<Action<FusionRequestOptions>> RequestOptionsModifiers { get; } = [];

    public List<Action<IServiceProvider, IServiceCollection>> SchemaServiceModifiers { get; } = [];

    public List<Action<IList<RequestMiddlewareConfiguration>>> PipelineModifiers { get; } = [];

    public List<Action<IServiceProvider, IFeatureCollection>> SchemaFeaturesModifiers { get; } = [];
}
