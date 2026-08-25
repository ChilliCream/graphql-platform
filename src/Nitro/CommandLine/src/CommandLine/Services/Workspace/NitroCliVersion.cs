using System.Reflection;

namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// Resolves the running Nitro CLI's own exact version, for stamping onto a
/// session row the same way a coding harness's hook adapters stamp their
/// own harness version.
/// </summary>
internal static class NitroCliVersion
{
    private static readonly Lazy<string> LazyCurrent = new(Resolve);

    /// <summary>
    /// The value of this assembly's <see cref="AssemblyInformationalVersionAttribute"/>,
    /// or the empty string when the attribute is absent.
    /// </summary>
    public static string Current => LazyCurrent.Value;

    private static string Resolve()
        => typeof(NitroCliVersion).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion
            ?? string.Empty;
}
