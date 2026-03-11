namespace HotChocolate.Data.Projections;

public readonly struct ProjectionFieldHandlerConfiguration
{
    private readonly Func<ProjectionProviderContext, IProjectionFieldHandler>? _factory;
    private readonly IProjectionFieldHandler? _instance;

    public ProjectionFieldHandlerConfiguration(IProjectionFieldHandler instance)
    {
        _instance = instance;
    }

    public ProjectionFieldHandlerConfiguration(Func<ProjectionProviderContext, IProjectionFieldHandler> factory)
    {
        _factory = factory;
    }

    public IProjectionFieldHandler Create(ProjectionProviderContext context)
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
