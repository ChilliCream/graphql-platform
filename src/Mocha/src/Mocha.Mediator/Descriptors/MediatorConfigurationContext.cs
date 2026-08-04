namespace Mocha.Mediator;

internal sealed class MediatorConfigurationContext(
    MediatorOptions options,
    IServiceProvider services,
    IFeatureCollection features) : IMediatorConfigurationContext
{
    public IServiceProvider Services => services;

    public MediatorOptions Options => options;

    public IFeatureCollection Features => features;
}
