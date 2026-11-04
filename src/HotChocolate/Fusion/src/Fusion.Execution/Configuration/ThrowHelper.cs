using System.Collections.Immutable;

namespace HotChocolate.Fusion.Configuration;

internal static class ThrowHelper
{
    public static InvalidOperationException UnsupportedGatewayFormats(
        ImmutableArray<Version> gatewayFormats,
        Version maxSupportedGatewayFormat)
        => new(
            $"The Fusion archive contains gateway formats {string.Join(", ", gatewayFormats)}, but this gateway supports up to {maxSupportedGatewayFormat}. Upgrade the gateway to load this archive.");
}
