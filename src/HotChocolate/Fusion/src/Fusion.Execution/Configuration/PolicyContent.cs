using HotChocolate.Fusion.Execution;

namespace HotChocolate.Fusion.Configuration;

/// <summary>
/// Represents the source content of a single policy read from a Fusion package.
/// </summary>
/// <param name="Name">The full policy name, which is the rule path without the <c>data.</c> prefix.</param>
/// <param name="Kind">The policy language the content is written in.</param>
/// <param name="Source">The UTF-8 encoded policy source.</param>
/// <param name="Requirements">The parts of the evaluation input the policy declares it reads.</param>
/// <param name="Digest">
/// The content digest of the policy taken from the package manifest, used to detect changes.
/// </param>
public sealed record PolicyContent(
    string Name,
    PolicyContentType Kind,
    ReadOnlyMemory<byte> Source,
    PolicyRequirements Requirements,
    ReadOnlyMemory<byte> Digest);
