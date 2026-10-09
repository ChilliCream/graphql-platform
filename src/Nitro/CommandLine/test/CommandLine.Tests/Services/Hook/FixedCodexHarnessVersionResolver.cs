using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Tests.Hook;

internal sealed class FixedCodexHarnessVersionResolver(string version = "") : ICodexHarnessVersionResolver
{
    public string Resolve(string sessionId) => version;
}
