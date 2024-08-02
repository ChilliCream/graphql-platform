using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.ApolloFederation;

internal static class FederationVersionExtensions
{
    private static readonly Dictionary<Uri, FederationVersion> _uriToVersion = new()
    {
        [new Uri(FederationVersionUrls.Federation20)] = FederationVersion.Federation20,
        [new Uri(FederationVersionUrls.Federation21)] = FederationVersion.Federation21,
        [new Uri(FederationVersionUrls.Federation22)] = FederationVersion.Federation22,
        [new Uri(FederationVersionUrls.Federation23)] = FederationVersion.Federation23,
        [new Uri(FederationVersionUrls.Federation24)] = FederationVersion.Federation24,
        [new Uri(FederationVersionUrls.Federation25)] = FederationVersion.Federation25,
        [new Uri(FederationVersionUrls.Federation26)] = FederationVersion.Federation26,
        [new Uri(FederationVersionUrls.Federation27)] = FederationVersion.Federation27,
    };

    private static readonly Dictionary<FederationVersion, Uri> _versionToUri = new()
    {
        [FederationVersion.Federation20] = new(FederationVersionUrls.Federation20),
        [FederationVersion.Federation21] = new(FederationVersionUrls.Federation21),
        [FederationVersion.Federation22] = new(FederationVersionUrls.Federation22),
        [FederationVersion.Federation23] = new(FederationVersionUrls.Federation23),
        [FederationVersion.Federation24] = new(FederationVersionUrls.Federation24),
        [FederationVersion.Federation25] = new(FederationVersionUrls.Federation25),
        [FederationVersion.Federation26] = new(FederationVersionUrls.Federation26),
        [FederationVersion.Federation27] = new(FederationVersionUrls.Federation27),
    };

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

    public static Uri ToUrl(this FederationVersion version)
    {
        if (_versionToUri.TryGetValue(version, out var url))
        {
            return url;
        }

        // TODO : resources
        throw new ArgumentException("The federation version is not supported.", nameof(version));
    }

    public static FederationVersion ToVersion(this Uri url)
    {
        if (_uriToVersion.TryGetValue(url, out var version))
        {
            return version;
        }

        // TODO : resources
        throw new ArgumentException("The federation url is not supported.", nameof(url));
    }

    public static bool TryToVersion(this Uri url, out FederationVersion version)
        => _uriToVersion.TryGetValue(url, out version);
}
