namespace HotChocolate.Fusion.Configuration;

/// <summary>
/// Represents the source content of a single policy read from a Fusion package.
/// </summary>
/// <param name="Name">The policy name.</param>
/// <param name="Kind">The policy language the content is written in.</param>
/// <param name="Source">The UTF-8 encoded policy source.</param>
/// <param name="Requirements">The UTF-8 encoded GraphQL data requirements source.</param>
/// <param name="Digest">
/// The content digest of the policy taken from the package manifest, used to detect changes.
/// </param>
public sealed record PolicyContent(
    string Name,
    PolicyContentType Kind,
    ReadOnlyMemory<byte> Source,
    ReadOnlyMemory<byte> Requirements,
    ReadOnlyMemory<byte> Digest);
