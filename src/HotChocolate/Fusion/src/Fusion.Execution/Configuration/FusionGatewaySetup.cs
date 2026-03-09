using HotChocolate.Execution;
using HotChocolate.Features;
using HotChocolate.Fusion.Execution;
using HotChocolate.Fusion.Execution.Clients;
using HotChocolate.Fusion.Planning;
using HotChocolate.Validation;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Configuration;

internal sealed class FusionGatewaySetup
{
    public Func<IServiceProvider, IFusionConfigurationProvider>? DocumentProvider { get; set; }

    public List<Action<FusionOptions>> OptionsModifiers { get; } = [];

    public List<Action<FusionRequestOptions>> RequestOptionsModifiers { get; } = [];

    public List<Action<OperationPlannerOptions>> PlannerOptionsModifiers { get; } = [];

    public List<Action<FusionParserOptions>> ParserOptionsModifiers { get; } = [];

    public List<Action<IServiceProvider, IServiceCollection>> SchemaServiceModifiers { get; } = [];

    public List<Action<IList<RequestMiddlewareConfiguration>>> PipelineModifiers { get; } = [];

    public List<Action<IServiceProvider, IFeatureCollection>> SchemaFeaturesModifiers { get; } = [];

    public List<Action<IServiceProvider, DocumentValidatorBuilder>> DocumentValidatorBuilderModifiers { get; } = [];

    public List<Func<IServiceProvider, ISourceSchemaClientConfiguration>> ClientConfigurationModifiers { get; } = [];
}
