using ChilliCream.Nitro.CommandLine.Commands.Agent;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Services.Memory;
using ChilliCream.Nitro.CommandLine.Services.Notify;
using ChilliCream.Nitro.CommandLine.Services.Workspace;
using ChilliCream.Nitro.CommandLine.Tests.Console;
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
/// Covers <see cref="AgentTuiLauncher"/>'s unified actor wiring and daemon,
/// and shutdown lifecycle.
/// </summary>
public sealed class AgentTuiLauncherTests
{
    private static readonly DateTimeOffset s_now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static string RenderToText(TuiShell shell, int width = 80)
    {
        var console = new TestConsole().Width(width);
        console.Write(shell.Render());
        return console.Output;
    }

    [Fact]
    public void BuildMailTab_Should_OpenWithoutAnActor()
    {
        var store = new FakeMailStore();

        var mailTab = AgentTuiLauncher.BuildMailTab(
            store,
            new Tui.Agents.FakeAgentRegistry(),
            new FakeTimeProvider(s_now), TestContext.Current.CancellationToken);

        var mailMode = Assert.IsType<MailMode>(mailTab.RootMode);
        Assert.Null(mailMode.State.Actor);
    }

    [Fact]
    public void BuildMailTab_Should_HostAWorkingMailMode()
    {
        // arrange
        var store = new FakeMailStore();

        // act
        var mailTab = AgentTuiLauncher.BuildMailTab(
            store, new Tui.Agents.FakeAgentRegistry(), new FakeTimeProvider(s_now),
            TestContext.Current.CancellationToken);

        // assert
        Assert.IsType<MailMode>(mailTab.RootMode);
        Assert.Equal("Mail", mailTab.Title);
    }

    [Fact]
    public void BuildTabs_Should_RegisterTabsInOrderTasksMailAgentsMemory_When_AllStoresAreGiven()
    {
        // arrange
        var taskStore = new FakeTaskStore();
        var mailStore = new FakeMailStore();
        var agentRegistry = new Tui.Agents.FakeAgentRegistry();
        var timeProvider = new FakeTimeProvider(s_now);

        var tempRoot = Directory.CreateTempSubdirectory("nitro-agent-tui-launcher-tests");

        try
        {
            var workingDirectory = Path.Combine(tempRoot.FullName, "acme");
            Directory.CreateDirectory(workingDirectory);
            var memoryStore = new MemoryStore(
                new Agents.TestFileSystem(workingDirectory),
                timeProvider,
                new AgentDatabase());

            var agentStore = new Tui.Agents.FakeAgentStore(timeProvider);

            var tabs = AgentTuiLauncher.BuildTabs(
                taskStore,
                mailStore,
                memoryStore,
                agentRegistry,
                agentStore,
                timeProvider,
                TestContext.Current.CancellationToken);

            var shell = new TuiShell(
                tabs,
                80,
                24,
                agentStore: agentStore,
                mailStore: mailStore,
                memoryStore: memoryStore,
                timeProvider: timeProvider,
                tasksTabIndex: 0,
                searchMode: new SearchMode(taskStore),
                treeView: new DependencyTreeView(taskStore, rootId: ""),
                store: taskStore,
                actor: "tasks-actor");

            // act
            var text = RenderToText(shell);

            // assert
            var tasksIndex = text.IndexOf("[T]asks", StringComparison.Ordinal);
            var mailIndex = text.IndexOf("[M]ail", StringComparison.Ordinal);
            var agentsIndex = text.IndexOf("[A]gents", StringComparison.Ordinal);
            var memoryIndex = text.IndexOf("M[e]mory", StringComparison.Ordinal);

            Assert.True(tasksIndex >= 0 && mailIndex > tasksIndex && agentsIndex > mailIndex && memoryIndex > agentsIndex);
        }
        finally
        {
            tempRoot.Delete(recursive: true);
        }
    }

    [Theory]
    [InlineData("Ready", "mail-wake:ready")]
    [InlineData("Standby", "mail-wake:standby")]
    [InlineData("Degraded", "mail-wake:degraded")]
    [InlineData("Stopping", "mail-wake:stopping")]
    public void Render_Should_ShowTheMailWakeDaemonBadge_When_AStateProviderIsGiven(
        string stateName, string expectedBadge)
    {
        // arrange
        var state = Enum.Parse<MailWakeDaemonState>(stateName);
        const int width = 160;
        var taskStore = new FakeTaskStore();
        var loader = new BoardDataLoader(taskStore, new FakeTimeProvider(s_now));
        var boardMode = new BoardMode(loader);
        var time = new FakeTimeProvider(s_now);
        var shell = new TuiShell(
            new KeyDispatcher(KeyMap.CreateDefaultGlobal()),
            boardMode,
            width,
            24,
            agentStore: new Tui.Agents.FakeAgentStore(time),
            mailStore: new FakeMailStore(),
            memoryStore: new Tui.Agents.FakeMemoryStore(),
            timeProvider: time,
            actor: "tasks-actor",
            mailWakeDaemonState: () => state);

        // act
        var text = RenderToText(shell, width);

        // assert
        Assert.Contains(expectedBadge, text);
    }

    [Fact]
    public async Task RunAsync_Should_StartAndStopTheMailWakeDaemonCoordinator_AroundTheApplicationLoop()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var tempRoot = Directory.CreateTempSubdirectory("nitro-agent-tui-launcher-coordinator-tests");

        try
        {
            var workingDirectory = Path.Combine(tempRoot.FullName, "acme");
            Directory.CreateDirectory(workingDirectory);
            var workspaceDirectory = AgentWorkspace.GetDirectory(workingDirectory);
            Directory.CreateDirectory(workspaceDirectory);

            var memoryStore = new MemoryStore(
                new Agents.TestFileSystem(workingDirectory),
                new FakeTimeProvider(s_now),
                new AgentDatabase());

            var outConsole = new TestConsole();
            outConsole.Profile.Capabilities.Interactive = true;
            outConsole.Profile.Width = 80;
            outConsole.Profile.Height = 24;

            var console = new NitroConsole(
                outConsole,
                new TestConsole(),
                new SnapshotActivitySinkFactory());

            var coordinator = new Mock<IMailWakeDaemonCoordinator>();
            coordinator.Setup(x => x.StartAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            coordinator.Setup(x => x.StopAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            coordinator.SetupGet(x => x.Status).Returns(MailWakeDaemonStatus.Initial);

            using var runCts = new CancellationTokenSource();

            // act
            var runTask = AgentTuiLauncher.RunAsync(
                console,
                new FakeTaskStore(),
                new FakeMailStore(),
                memoryStore,
                new Tui.Agents.FakeAgentRegistry(),
                new Tui.Agents.FakeAgentStore(new FakeTimeProvider(s_now)),
                new FakeTimeProvider(s_now),
                workspaceDirectory,
                coordinator.Object,
                runCts.Token);

            await Task.Delay(TimeSpan.FromMilliseconds(150), cancellationToken);
            await runCts.CancelAsync();
            var exitCode = await runTask.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken);

            // assert
            Assert.Equal(ExitCodes.Success, exitCode);
            coordinator.Verify(x => x.StartAsync(It.IsAny<CancellationToken>()), Times.Once);
            coordinator.Verify(x => x.StopAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
        finally
        {
            tempRoot.Delete(recursive: true);
        }
    }

    [Fact]
    public async Task RunAsync_Should_StillStopTheMailWakeDaemonCoordinator_When_CancellationIsAlreadyRequestedBeforeTheLoopStarts()
    {
        // arrange
        // Pass an already-cancelled token to the launcher.
        var cancellationToken = TestContext.Current.CancellationToken;
        var tempRoot = Directory.CreateTempSubdirectory("nitro-agent-tui-launcher-coordinator-tests");

        try
        {
            var workingDirectory = Path.Combine(tempRoot.FullName, "acme");
            Directory.CreateDirectory(workingDirectory);
            var workspaceDirectory = AgentWorkspace.GetDirectory(workingDirectory);
            Directory.CreateDirectory(workspaceDirectory);

            var memoryStore = new MemoryStore(
                new Agents.TestFileSystem(workingDirectory),
                new FakeTimeProvider(s_now),
                new AgentDatabase());

            var outConsole = new TestConsole();
            outConsole.Profile.Capabilities.Interactive = true;
            outConsole.Profile.Width = 80;
            outConsole.Profile.Height = 24;

            var console = new NitroConsole(
                outConsole,
                new TestConsole(),
                new SnapshotActivitySinkFactory());

            var coordinator = new Mock<IMailWakeDaemonCoordinator>();
            coordinator.Setup(x => x.StartAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            coordinator.Setup(x => x.StopAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            coordinator.SetupGet(x => x.Status).Returns(MailWakeDaemonStatus.Initial);

            using var alreadyCancelled = new CancellationTokenSource();
            await alreadyCancelled.CancelAsync();

            // act
            var exitCode = await AgentTuiLauncher.RunAsync(
                console,
                new FakeTaskStore(),
                new FakeMailStore(),
                memoryStore,
                new Tui.Agents.FakeAgentRegistry(),
                new Tui.Agents.FakeAgentStore(new FakeTimeProvider(s_now)),
                new FakeTimeProvider(s_now),
                workspaceDirectory,
                coordinator.Object,
                alreadyCancelled.Token).WaitAsync(TimeSpan.FromSeconds(5), cancellationToken);

            // assert
            Assert.Equal(ExitCodes.Success, exitCode);
            coordinator.Verify(x => x.StartAsync(It.IsAny<CancellationToken>()), Times.Once);
            coordinator.Verify(x => x.StopAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
        finally
        {
            tempRoot.Delete(recursive: true);
        }
    }
}
