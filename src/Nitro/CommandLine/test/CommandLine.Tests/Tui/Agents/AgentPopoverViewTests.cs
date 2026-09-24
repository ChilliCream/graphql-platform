using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Memory;
using ChilliCream.Nitro.CommandLine.Services.Tasks;
using ChilliCream.Nitro.CommandLine.Services.Workspace;
using ChilliCream.Nitro.CommandLine.Tests.Tui.Shell;
using ChilliCream.Nitro.CommandLine.Tui.Agents;
using Spectre.Console;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Agents;

/// <summary>
/// Exercises <see cref="AgentPopoverView"/>'s pure formatting: the header block, the three
/// participation sections, and the individual mail, ticket, and memory row formats.
/// </summary>
public sealed class AgentPopoverViewTests
{
    private static readonly DateTimeOffset s_now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly (AgentPopoverSection Section, bool IsShowMore, int ItemIndex) s_noSelection =
        (AgentPopoverSection.Memory, true, -1);

    private static AgentRow CreateRow(
        string role = "",
        string? harness = "claude-code",
        string harnessVersion = "2.1.0",
        string? sessionId = "session-a",
        DateTimeOffset? endedAt = null) => new()
        {
            Name = "agent-a",
            Role = role,
            Harness = harness,
            HarnessVersion = harnessVersion,
            SessionId = sessionId,
            Cwd = "",
            WorkspacePath = "",
            RegisteredAt = s_now,
            StartedAt = s_now,
            LastSeenAt = s_now,
            EndedAt = endedAt,
            EndpointKind = AgentSessionEndpointKind.ClaudePeer,
            EndpointAddr = "peer-1",
            BlockBudgetUsed = 0,
            AnnouncementPending = false,
            IdlePushArmed = false
        };

    private static MailThreadSummary CreateMailSummary(
        string threadId, string subject, int unreadCount = 0) => new()
        {
            ThreadId = threadId,
            Subject = subject,
            MessageCount = 3,
            LastMessageAt = s_now,
            LastSender = "felix",
            LastRecipients = ["oscar"],
            BodyPreview = "",
            UnreadCount = unreadCount,
            ArchivedCount = 0
        };

    private static string PlainText(AgentPopoverBuiltLines built) =>
        string.Join('\n', built.Lines.Select(Markup.Remove));

    [Fact]
    public void BuildLines_Should_ShowHarnessAndSessionId_When_TheAgentIsAHookAgent()
    {
        // arrange
        var row = CreateRow();

        // act
        var built = AgentPopoverView.BuildLines(row, [], [], [], s_now, 80, s_noSelection);
        var text = PlainText(built);

        // assert
        Assert.Contains("Harness: claude-code 2.1.0", text);
        Assert.Contains("Session id: session-a", text);
    }

    [Fact]
    public void BuildLines_Should_ShowDashesForHarnessAndSessionId_When_TheAgentIsLoginOnly()
    {
        // arrange
        var row = CreateRow(harness: null, harnessVersion: "", sessionId: null);

        // act
        var built = AgentPopoverView.BuildLines(row, [], [], [], s_now, 80, s_noSelection);
        var text = PlainText(built);

        // assert
        Assert.Contains("Harness: -", text);
        Assert.Contains("Session id: -", text);
    }

    [Fact]
    public void BuildLines_Should_OmitTheVersion_When_HarnessVersionIsEmpty()
    {
        // arrange
        var row = CreateRow(harnessVersion: "");

        // act
        var built = AgentPopoverView.BuildLines(row, [], [], [], s_now, 80, s_noSelection);
        var text = PlainText(built);

        // assert
        Assert.Contains("Harness: claude-code\n", text);
    }

    [Fact]
    public void BuildLines_Should_ShowTheEndedRow_When_EndedAtIsSet()
    {
        // arrange
        var row = CreateRow(endedAt: s_now.AddHours(-1));

        // act
        var built = AgentPopoverView.BuildLines(row, [], [], [], s_now, 80, s_noSelection);
        var text = PlainText(built);

        // assert
        Assert.Contains("Ended: 1h ago", text);
    }

    [Fact]
    public void BuildLines_Should_ShowEmptySectionMessages_When_ThereIsNoParticipation()
    {
        // arrange
        var row = CreateRow();

        // act
        var built = AgentPopoverView.BuildLines(row, [], [], [], s_now, 80, s_noSelection);
        var text = PlainText(built);

        // assert
        Assert.Contains("No mail yet", text);
        Assert.Contains("No tickets yet", text);
        Assert.Contains("No memory yet", text);
    }

    [Fact]
    public void BuildLines_Should_ShowARowPerTicket_When_ThereAreThreeTickets()
    {
        // arrange
        var row = CreateRow();
        var tickets = new[]
        {
            TaskItemBuilder.Create("a1", "First"),
            TaskItemBuilder.Create("a2", "Second"),
            TaskItemBuilder.Create("a3", "Third")
        };

        // act
        var built = AgentPopoverView.BuildLines(row, [], tickets, [], s_now, 80, s_noSelection);
        var text = PlainText(built);

        // assert
        Assert.Contains("a1  open  First", text);
        Assert.Contains("a2  open  Second", text);
        Assert.Contains("a3  open  Third", text);
    }

    [Fact]
    public void BuildLines_Should_MarkTheSelectedRowWithTheHighlightStyle_When_ItIsAMailRow()
    {
        // arrange
        var row = CreateRow();
        var mail = new[] { CreateMailSummary("t1", "Subject") };
        var selected = (AgentPopoverSection.Mail, IsShowMore: false, ItemIndex: 0);

        // act
        var built = AgentPopoverView.BuildLines(row, mail, [], [], s_now, 80, selected);
        var mailLine = built.Lines[built.SelectedLineIndex];

        // assert
        Assert.Contains("Subject", Markup.Remove(mailLine));
        Assert.NotEqual(Markup.Remove(mailLine), mailLine);
    }

    [Fact]
    public void FormatMailRow_Should_ReturnPlainContent_When_TheThreadIsRead()
    {
        // arrange
        var summary = CreateMailSummary("t1", "Ship it", unreadCount: 0);

        // act
        var line = AgentPopoverView.FormatMailRow(summary, s_now, 80);

        // assert
        Assert.Equal("now  Ship it  felix -> oscar (3)", line);
    }

    [Fact]
    public void FormatMailRow_Should_WrapInAStyleTag_When_TheThreadIsUnreadForTheAgent()
    {
        // arrange
        var summary = CreateMailSummary("t1", "Ship it", unreadCount: 2);

        // act
        var line = AgentPopoverView.FormatMailRow(summary, s_now, 80);

        // assert
        Assert.StartsWith("[", line);
        Assert.Equal("now  Ship it  felix -> oscar (3)", Markup.Remove(line));
    }

    [Fact]
    public void FormatTicketRow_Should_ReturnPlainContent_When_TheTicketIsOpen()
    {
        // arrange
        var task = TaskItemBuilder.Create("a1", "Fix it");

        // act
        var line = AgentPopoverView.FormatTicketRow(task, 80);

        // assert
        Assert.Equal("a1  open  Fix it", line);
    }

    [Fact]
    public void FormatTicketRow_Should_WrapInAStyleTag_When_TheTicketIsClosed()
    {
        // arrange
        var task = TaskItemBuilder.Create("a1", "Fix it", TaskStates.Closed);

        // act
        var line = AgentPopoverView.FormatTicketRow(task, 80);

        // assert
        Assert.StartsWith("[", line);
        Assert.Equal("a1  closed  Fix it", Markup.Remove(line));
    }

    [Fact]
    public void FormatTicketRow_Should_WrapInAStyleTag_When_TheTicketIsArchived()
    {
        // arrange
        var task = TaskItemBuilder.Create("a1", "Fix it", TaskStates.Archived);

        // act
        var line = AgentPopoverView.FormatTicketRow(task, 80);

        // assert
        Assert.StartsWith("[", line);
        Assert.Equal("a1  archived  Fix it", Markup.Remove(line));
    }

    [Fact]
    public void FormatMemoryRow_Should_IncludeTheType_When_TheEntryIsCurated()
    {
        // arrange
        var entry = new MemoryParticipationEntry(
            MemoryParticipationKind.Curated, "m1", "fact", [], "First line.\nSecond line.", s_now);

        // act
        var line = AgentPopoverView.FormatMemoryRow(entry, s_now, 80);

        // assert
        Assert.Equal("curated fact  now  First line.", line);
    }

    [Fact]
    public void FormatMemoryRow_Should_OmitTheType_When_TheEntryIsAJournalNote()
    {
        // arrange
        var entry = new MemoryParticipationEntry(MemoryParticipationKind.Journal, "j1", null, [], "A note.", s_now);

        // act
        var line = AgentPopoverView.FormatMemoryRow(entry, s_now, 80);

        // assert
        Assert.Equal("journal  now  A note.", line);
    }

    [Fact]
    public void Highlight_Should_WrapContentInTheSelectionStyle_When_Called()
    {
        // arrange
        const string line = "plain text";

        // act
        var highlighted = AgentPopoverView.Highlight(line);

        // assert
        Assert.StartsWith("[", highlighted);
        Assert.Equal(line, Markup.Remove(highlighted));
    }
}
