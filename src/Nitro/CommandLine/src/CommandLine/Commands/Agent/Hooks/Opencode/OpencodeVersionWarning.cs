using ChilliCream.Nitro.CommandLine.Helpers;

namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Hooks.Opencode;

internal static class OpencodeVersionWarning
{
    private static readonly Version s_minimumSupportedVersion = new(1, 18);

    public static async Task WriteAsync(
        INitroConsole console,
        IOpencodeVersionResolver versionResolver,
        CancellationToken cancellationToken)
    {
        var version = await versionResolver.ResolveAsync(cancellationToken);

        if (!console.IsHumanReadable
            || version?.CompareTo(s_minimumSupportedVersion) >= 0)
        {
            return;
        }

        var message = version is null
            ? "Could not determine the Opencode version. Continuing with Nitro hooks."
            : $"Opencode {version} is below the recommended 1.18. Continuing with Nitro hooks.";

        console.MarkupLine(message.AsWarning());
    }
}
