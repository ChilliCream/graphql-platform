namespace ChilliCream.Nitro.CommandLine.Tests.Agents;

internal sealed record FakeOpencodePushCall(string ServerUrl, string SessionId, string Text, string? Secret);
