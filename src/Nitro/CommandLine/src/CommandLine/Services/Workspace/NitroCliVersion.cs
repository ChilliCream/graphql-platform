using System.Reflection;

namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// Exposes the running Nitro CLI assembly's informational version.
/// </summary>
internal static class NitroCliVersion
{
    private static readonly Lazy<string> s_lazyCurrent = new(Resolve);

    /// <summary>
    /// The value of this assembly's <see cref="AssemblyInformationalVersionAttribute"/>,
    /// or the empty string when the attribute is absent.
    /// </summary>
    public static string Current => s_lazyCurrent.Value;

    private static string Resolve()
        => typeof(NitroCliVersion).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion
            ?? string.Empty;
}
