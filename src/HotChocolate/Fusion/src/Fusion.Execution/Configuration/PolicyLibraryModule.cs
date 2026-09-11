namespace HotChocolate.Fusion.Configuration;

/// <summary>
/// A Rego module that is compiled alongside every policy set but never becomes a decision: either a
/// bundle-wide shared library or a helper module local to one policy package.
/// </summary>
/// <param name="Name">The module's canonical relative path within the bundle, used as its compiled module name.</param>
/// <param name="Source">The module source as UTF-8 encoded bytes.</param>
/// <param name="Digest">The content digest of the module, used to detect changes.</param>
public sealed record PolicyLibraryModule(
    string Name,
    ReadOnlyMemory<byte> Source,
    ReadOnlyMemory<byte> Digest);
