using ChilliCream.Nitro.CommandLine.Commands.Agent;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Tasks;
using ChilliCream.Nitro.CommandLine.Tests.Tui.Board;
using ChilliCream.Nitro.CommandLine.Tests.Tui.Mail;
using ChilliCream.Nitro.CommandLine.Tui.Board;
using ChilliCream.Nitro.CommandLine.Tui.Input;
using ChilliCream.Nitro.CommandLine.Tui.Mail;
using ChilliCream.Nitro.CommandLine.Tui.Search;
using ChilliCream.Nitro.CommandLine.Tui.Shell;
using ChilliCream.Nitro.CommandLine.Tui.Tree;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Spectre.Console.Testing;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Agent;

/// <summary>
/// Covers <see cref="AgentTuiLauncher.BuildMailTab"/>: the task actor and
/// the mail actor resolve independently from their own environment
/// variables, and an invalid mail actor (an <see cref="ExitException"/>
/// from <see cref="MailActor.Resolve"/>) renders the Mail tab in an error
/// state instead of preventing the shell from opening on the Tasks tab.
/// </summary>
public sealed class AgentTuiLauncherTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static Mock<IEnvironmentVariableProvider> CreateEnvironment(
        string? mailActor = null, string? taskActor = null)
    {
        var environment = new Mock<IEnvironmentVariableProvider>();
        environment
            .Setup(x => x.GetEnvironmentVariable(MailActor.EnvironmentVariableName))
            .Returns(mailActor);
        environment
            .Setup(x => x.GetEnvironmentVariable(MailActor.FallbackEnvironmentVariableName))
            .Returns(taskActor);
        return environment;
    }

    private static TuiShell BuildShell(TuiTab mailTab)
    {
        var taskStore = new FakeTaskStore();
        var loader = new BoardDataLoader(taskStore, new FakeTimeProvider(Now));
        var boardMode = new BoardMode(loader);
        var tasksTab = new TuiTab("Tasks", boardMode, new KeyDispatcher(KeyMap.CreateDefaultGlobal()));

        return new TuiShell(
            [tasksTab, mailTab],
            80,
            24,
            tasksTabIndex: 0,
            new SearchMode(taskStore),
            new DependencyTreeView(taskStore, rootId: ""),
            taskStore,
            actor: "tasks-actor");
    }

    private static string RenderToText(TuiShell shell)
    {
        var console = new TestConsole().Width(80);
        console.Write(shell.Render());
        return console.Output;
    }

    [Fact]
    public void BuildMailTab_Should_ResolveMailActor_FromItsOwnVariable_IndependentlyOfTaskActor()
    {
        // arrange: NITRO_TASK_ACTOR and NITRO_MAIL_ACTOR diverge.
        var environment = CreateEnvironment(mailActor: "mail-actor", taskActor: "task-actor");
        var store = new FakeMailStore();

        // act
        var taskActor = TaskActor.Resolve(null, environment.Object);
        var mailTab = AgentTuiLauncher.BuildMailTab(store, new FakeTimeProvider(Now), environment.Object);

        // assert: each actor came from its own variable, not the other's.
        Assert.Equal("task-actor", taskActor);
        var mailMode = Assert.IsType<MailMode>(mailTab.RootMode);
        Assert.Equal("mail-actor", mailMode.State.Actor);
    }

    [Fact]
    public void BuildMailTab_Should_HostAnErrorState_When_MailActorFallsBackToAnInvalidTaskActor()
    {
        // arrange: no NITRO_MAIL_ACTOR, so MailActor.Resolve falls back to
        // NITRO_TASK_ACTOR, whose value fails MailAgentName.Normalize (the
        // dot is not a valid agent-name character); TaskActor.Resolve has
        // no such validation and accepts the same value unchanged.
        var environment = CreateEnvironment(mailActor: null, taskActor: "pascal.senn");
        var store = new FakeMailStore();

        // act
        var taskActor = TaskActor.Resolve(null, environment.Object);
        var mailTab = AgentTuiLauncher.BuildMailTab(store, new FakeTimeProvider(Now), environment.Object);

        // assert: the tasks actor is unaffected by the mail actor's
        // validation failure, and the mail tab hosts the error state
        // instead of a working MailMode, labeled exactly "Mail" (no badge).
        Assert.Equal("pascal.senn", taskActor);
        Assert.IsType<MailUnavailableMode>(mailTab.RootMode);
        Assert.Equal("Mail", mailTab.Title);
    }

    [Fact]
    public void BuildMailTab_Should_HostAWorkingMailMode_When_TheActorIsValid()
    {
        // arrange
        var environment = CreateEnvironment(mailActor: "valid-actor", taskActor: null);
        var store = new FakeMailStore();

        // act
        var mailTab = AgentTuiLauncher.BuildMailTab(store, new FakeTimeProvider(Now), environment.Object);

        // assert: today's behavior, unchanged: a working MailMode, badge-free
        // title until a refresh reports unread messages.
        Assert.IsType<MailMode>(mailTab.RootMode);
        Assert.Equal("Mail", mailTab.Title);
    }

    [Fact]
    public void Shell_Should_StillOpenOnTheTasksTab_When_TheMailTabIsInTheErrorState()
    {
        // arrange: the same invalid-fallback scenario, this time wired all
        // the way through the tabbed TuiShell the way AgentTuiLauncher.RunAsync
        // builds it.
        var environment = CreateEnvironment(mailActor: null, taskActor: "pascal.senn");
        var mailTab = AgentTuiLauncher.BuildMailTab(
            new FakeMailStore(), new FakeTimeProvider(Now), environment.Object);

        // act: construction alone must not throw despite the Mail tab's
        // actor resolution failure.
        var shell = BuildShell(mailTab);
        var text = RenderToText(shell);

        // assert: the shell opened and both tabs render (the mail tab's
        // exact title, "Mail" with no unread badge, is covered at the
        // BuildMailTab level above).
        Assert.Contains("Tasks", text);
        Assert.Contains("Mail", text);
    }
}
