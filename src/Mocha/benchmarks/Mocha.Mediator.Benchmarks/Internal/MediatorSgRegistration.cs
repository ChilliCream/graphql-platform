using Microsoft.Extensions.DependencyInjection;

namespace Benchmarks.Internal;

// In this namespace, services.AddMediator() unambiguously resolves
// to martinothamar's Mediator source generator extension method.
internal static class MediatorSgRegistration
{
    public static void Register(IServiceCollection services)
        => services.AddMediator();
}
