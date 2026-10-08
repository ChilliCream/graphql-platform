namespace HotChocolate.Serialization;

internal static class ThrowHelper
{
    public static ArgumentOutOfRangeException GraphQLSpecVersions_UnknownVersion(
        GraphQLSpecVersion version)
        => new(nameof(version), version, null);
}
