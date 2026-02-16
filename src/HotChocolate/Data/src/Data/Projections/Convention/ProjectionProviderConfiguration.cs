using HotChocolate.Types;

namespace HotChocolate.Data.Projections;

public class ProjectionProviderConfiguration : IHasScope
{
    public string? Scope { get; set; }

    public IList<ProjectionFieldHandlerConfiguration> FieldHandlerConfigurations { get; } = [];

    public IList<ProjectionFieldInterceptorConfiguration> FieldInterceptorConfigurations { get; } = [];

    public IList<ProjectionOptimizerConfiguration> OptimizerConfigurations { get; } = [];
}
