namespace HotChocolate.Execution.Configuration;

internal sealed class DefaultApplicationServiceProvider
    : IApplicationServiceProvider
{
    private readonly IServiceProvider _applicationServices;

    public DefaultApplicationServiceProvider(IServiceProvider applicationServices)
    {
        _applicationServices = applicationServices ??
            throw new ArgumentNullException(nameof(applicationServices));
    }

    public object? GetService(Type serviceType) =>
        _applicationServices.GetService(serviceType);
}
