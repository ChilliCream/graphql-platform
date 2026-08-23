using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace ChilliCream.Nitro.CommandLine.Services.Hook;

/// <summary>
/// The embedded <c>extension.mjs</c> asset (perles-net-k3j.16) this CLI
/// installs to <c>.github/extensions/nitro-mail/extension.mjs</c>. The file
/// carries its own <c>// nitro-mail-extension-version: N</c> header comment,
/// but the installer's overwrite-refusal check compares content hashes, not
/// that header, so a hand-edited file keeping the header intact is still
/// refused.
/// </summary>
internal static class CopilotExtensionAsset
{
    private const string ResourceName = "CopilotExtension.extension.mjs";

    /// <summary>
    /// The current asset version, ships as-is until a future change bumps
    /// it and adds this version's hash to <see cref="KnownPriorHashes"/>.
    /// </summary>
    public const int CurrentVersion = 1;

    /// <summary>
    /// Content hashes of every asset version this CLI has ever shipped,
    /// EXCLUDING the current one (which is always known, computed live from
    /// <see cref="Content"/>). Empty for version 1: there is no prior
    /// version to recognize yet. When version 2 ships, add version 1's
    /// frozen hash here so an install over a version-1 file upgrades
    /// silently instead of requiring <c>--force</c>.
    /// </summary>
    public static readonly IReadOnlyList<string> KnownPriorHashes = [];

    public static string Content { get; } = ReadEmbeddedContent();

    public static string ContentHash { get; } = Hash(Content);

    /// <summary>
    /// True when <paramref name="onDiskContent"/> matches this CLI's current
    /// asset content or any recognized prior version - i.e. it is safe to
    /// overwrite without <c>--force</c>. False for anything else: absent
    /// content, a hand-edited file, or a completely foreign file.
    /// </summary>
    public static bool IsKnownVersion(string onDiskContent)
    {
        var onDiskHash = Hash(onDiskContent);

        return onDiskHash == ContentHash || KnownPriorHashes.Contains(onDiskHash);
    }

    public static string Hash(string content)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(content)));

    private static string ReadEmbeddedContent()
    {
        var assembly = Assembly.GetExecutingAssembly();

        using var stream = assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException(
                $"Embedded resource '{ResourceName}' was not found in '{assembly.FullName}'. "
                + "Check the EmbeddedResource LogicalName in Nitro.CommandLine.csproj.");

        using var reader = new StreamReader(stream, Encoding.UTF8);

        return reader.ReadToEnd();
    }
}
