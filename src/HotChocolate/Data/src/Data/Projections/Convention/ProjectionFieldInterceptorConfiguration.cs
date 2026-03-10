namespace HotChocolate.Data.Projections;

public readonly struct ProjectionFieldInterceptorConfiguration
{
    private readonly Func<ProjectionProviderContext, IProjectionFieldInterceptor>? _factory;
    private readonly IProjectionFieldInterceptor? _instance;

    public ProjectionFieldInterceptorConfiguration(IProjectionFieldInterceptor instance)
    {
        _instance = instance;
    }

    public ProjectionFieldInterceptorConfiguration(Func<ProjectionProviderContext, IProjectionFieldInterceptor> factory)
    {
        _factory = factory;
    }

    public IProjectionFieldInterceptor Create(ProjectionProviderContext context)
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
