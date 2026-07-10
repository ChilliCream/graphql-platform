using Microsoft.Extensions.DependencyInjection;

namespace Mocha.Mediator.Benchmarks.Messaging;

internal static class MediatorSgHelper
{
    public static void Register(IServiceCollection services)
        => global::Benchmarks.Internal.MediatorSgRegistration.Register(services);
}
