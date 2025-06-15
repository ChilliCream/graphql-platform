using HotChocolate.Execution;
using HotChocolate.Features;
using HotChocolate.Fusion.Execution;
using HotChocolate.Validation;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Configuration;

internal sealed class FusionGatewaySetup
{
    public Func<IServiceProvider, IFusionSchemaDocumentProvider>? DocumentProvider { get; set; }

    public List<Action<FusionRequestOptions>> RequestOptionsModifiers { get; } = [];

    public List<Action<FusionParserOptions>> ParserOptionsModifiers { get; } = [];

    public List<Action<IServiceProvider, IServiceCollection>> SchemaServiceModifiers { get; } = [];

    public List<Action<IList<RequestMiddlewareConfiguration>>> PipelineModifiers { get; } = [];

    public List<Action<IServiceProvider, IFeatureCollection>> SchemaFeaturesModifiers { get; } = [];

    public List<Action<IServiceProvider, DocumentValidatorBuilder>> DocumentValidatorBuilderModifiers { get; } = [];
}
