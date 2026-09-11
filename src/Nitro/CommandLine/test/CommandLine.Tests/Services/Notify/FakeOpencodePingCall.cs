namespace ChilliCream.Nitro.CommandLine.Tests.Agents;

internal sealed record FakeOpencodePingCall(string ServerUrl, string SessionId, string? Secret);
