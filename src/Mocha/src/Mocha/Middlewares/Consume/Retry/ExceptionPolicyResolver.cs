using Microsoft.Extensions.DependencyInjection;

namespace Mocha;

/// <summary>
/// Resolves the exception policy that applies to messages received on a receive endpoint.
/// </summary>
internal static class ExceptionPolicyResolver
{
    /// <summary>
    /// Resolves the exception policy of the endpoint, its transport, or the bus, in that order of precedence.
    /// Returns <c>null</c> when none of them configures a policy.
    /// </summary>
    public static ExceptionPolicyFeature? Resolve(IServiceProvider services, ReceiveEndpoint endpoint)
    {
        if (endpoint.Features.TryGet(out ExceptionPolicyFeature? endpointFeature))
        {
            return endpointFeature;
        }

        if (endpoint.Transport.Features.TryGet(out ExceptionPolicyFeature? transportFeature))
        {
            return transportFeature;
        }

        return services.GetRequiredService<IFeatureCollection>().Get<ExceptionPolicyFeature>();
    }
}
