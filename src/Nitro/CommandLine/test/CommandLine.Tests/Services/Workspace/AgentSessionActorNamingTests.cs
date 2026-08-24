using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Tests.Agents;

/// <summary>
/// Exercises <see cref="AgentSessionActorNaming"/>: the harness-prefixed
/// generated actor name every unbound SessionStart falls back to.
/// </summary>
public sealed class AgentSessionActorNamingTests
{
    [Theory]
    [InlineData(AgentSessionHarness.ClaudeCode, "session-1", "claude-session-1")]
    [InlineData(AgentSessionHarness.Codex, "01a02e51-c257-75c3-b242-b56199a18839", "codex-01a02e51-c257-75c3-b242-b56199a18839")]
    [InlineData(AgentSessionHarness.Copilot, "b2535577-1f31-4eaa-8688-963b7953a657", "copilot-b2535577-1f31-4eaa-8688-963b7953a657")]
    public void Generate_Should_ReturnHarnessPrefixedName_When_Called(string harness, string sessionId, string expected)
    {
        // act
        var actor = AgentSessionActorNaming.Generate(harness, sessionId);

        // assert
        Assert.Equal(expected, actor);
    }

    [Fact]
    public void Generate_Should_ReturnTheSameName_When_CalledTwiceWithTheSameHarnessAndSessionId()
    {
        // act
        var first = AgentSessionActorNaming.Generate(AgentSessionHarness.ClaudeCode, "session-1");
        var second = AgentSessionActorNaming.Generate(AgentSessionHarness.ClaudeCode, "session-1");

        // assert
        Assert.Equal(first, second);
    }

    [Fact]
    public void Generate_Should_ReturnDistinctNames_When_SessionIdsDiffer()
    {
        // act
        var first = AgentSessionActorNaming.Generate(AgentSessionHarness.ClaudeCode, "session-1");
        var second = AgentSessionActorNaming.Generate(AgentSessionHarness.ClaudeCode, "session-2");

        // assert
        Assert.NotEqual(first, second);
    }

    [Fact]
    public void Generate_Should_ReturnDistinctNames_When_HarnessesDifferForTheSameSessionId()
    {
        // act
        var claude = AgentSessionActorNaming.Generate(AgentSessionHarness.ClaudeCode, "session-1");
        var codex = AgentSessionActorNaming.Generate(AgentSessionHarness.Codex, "session-1");
        var copilot = AgentSessionActorNaming.Generate(AgentSessionHarness.Copilot, "session-1");

        // assert
        Assert.Equal(3, new[] { claude, codex, copilot }.Distinct().Count());
    }

    [Fact]
    public void Generate_Should_SatisfyActorValidation_When_SessionIdContainsUppercaseOrInvalidCharacters()
    {
        // arrange: a session id shaped like nothing any real harness emits
        // today, deliberately carrying characters MailAgentName.Normalize
        // rejects, so the generated actor must sanitize rather than throw.
        const string sessionId = "Session/ID Weird.Value";

        // act
        var actor = AgentSessionActorNaming.Generate(AgentSessionHarness.ClaudeCode, sessionId);
        var normalized = MailAgentName.Normalize(actor);

        // assert
        Assert.Equal(actor, normalized);
    }
}
