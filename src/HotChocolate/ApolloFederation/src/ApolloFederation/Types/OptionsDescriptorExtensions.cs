using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.ApolloFederation;

internal static class OptionsDescriptorExtensions
{
    public static FederationVersion GetFederationVersion<T>(
        this IDescriptor<T> descriptor)
        where T : DefinitionBase
    {
        var contextData = descriptor.Extend().Context.ContextData;
        if (contextData.TryGetValue(Constants.WellKnownContextData.FederationVersion, out var value) &&
            value is FederationVersion version and > FederationVersion.Unknown)
        {
            return version;
        }

        // TODO : resources
        throw new InvalidOperationException("The configuration state is invalid.");
    }
}