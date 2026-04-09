namespace Mocha;

/// <summary>
/// Strongly-typed base class for topology resources that provides access to a specific configuration type.
/// </summary>
/// <typeparam name="T">The concrete topology configuration type.</typeparam>
public abstract class TopologyResource<T> : TopologyResource where T : TopologyConfiguration
{
    protected new T Configuration => (T)base.Configuration;

    protected sealed override void OnInitialize(TopologyConfiguration configuration)
    {
        Topology = configuration.Topology ?? throw ThrowHelper.TopologyRequired();

        OnInitialize((T)configuration);
    }

    protected abstract void OnInitialize(T configuration);

    protected sealed override void OnComplete(TopologyConfiguration configuration)
    {
        OnComplete((T)configuration);
    }

    protected abstract void OnComplete(T configuration);
}
