namespace ChilliCream.Nitro.CommandLine.Tests.Agents;

internal sealed record FakePingSessionExecutorCall(
    string ActorName, bool IsClaudePeer, bool IsOpencodeServer = false);
