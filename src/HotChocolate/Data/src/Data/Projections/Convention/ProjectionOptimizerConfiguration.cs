namespace HotChocolate.Data.Projections;

public readonly struct ProjectionOptimizerConfiguration
{
    private readonly Func<ProjectionProviderContext, IProjectionOptimizer>? _factory;
    private readonly IProjectionOptimizer? _instance;

    public ProjectionOptimizerConfiguration(IProjectionOptimizer instance)
    {
        _instance = instance;
    }

    public ProjectionOptimizerConfiguration(Func<ProjectionProviderContext, IProjectionOptimizer> factory)
    {
        _factory = factory;
    }

    public IProjectionOptimizer Create(ProjectionProviderContext context)
    {
        if (_instance is not null)
        {
            return _instance;
        }

        if (_factory is null)
        {
            throw new InvalidOperationException("Expected to have either a factory or an instance.");
        }

        return _factory(context);
    }
}
