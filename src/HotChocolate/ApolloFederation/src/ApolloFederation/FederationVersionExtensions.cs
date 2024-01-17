using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.ApolloFederation.Types;

internal static class FederationVersionExtensions
{
    public static FederationVersion GetFederationVersion<T>(
        this IDescriptor<T> descriptor)
        where T : DefinitionBase
    {
        var contextData = descriptor.Extend().Context.ContextData;
        if (contextData.TryGetValue(FederationContextData.FederationVersion, out var value) &&
            value is FederationVersion version and > FederationVersion.Unknown)
        {
            return version;
        }

        // TODO : resources
        throw new InvalidOperationException("The configuration state is invalid.");
    }
    
    public static FederationVersion GetFederationVersion(
        this IDescriptorContext context)
    {
        if (context.ContextData.TryGetValue(FederationContextData.FederationVersion, out var value) &&
            value is FederationVersion version and > FederationVersion.Unknown)
        {
            return version;
        }

        // TODO : resources
        throw new InvalidOperationException("The configuration state is invalid.");
    }
}