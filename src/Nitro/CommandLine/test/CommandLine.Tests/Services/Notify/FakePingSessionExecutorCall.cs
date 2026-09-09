namespace ChilliCream.Nitro.CommandLine.Tests.Agents;

internal sealed record FakePingSessionExecutorCall(
    string Harness, string SessionId, bool IsClaudePeer, bool IsOpencodeServer = false);
