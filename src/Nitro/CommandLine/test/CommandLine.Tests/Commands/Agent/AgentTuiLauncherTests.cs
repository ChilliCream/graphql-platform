using ChilliCream.Nitro.CommandLine.Commands.Agent;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Memory;
using ChilliCream.Nitro.CommandLine.Services.Tasks;
using ChilliCream.Nitro.CommandLine.Services.Workspace;
using ChilliCream.Nitro.CommandLine.Tests.Tui.Board;
using ChilliCream.Nitro.CommandLine.Tests.Tui.Mail;
using ChilliCream.Nitro.CommandLine.Tui.Agents;
using ChilliCream.Nitro.CommandLine.Tui.Board;
using ChilliCream.Nitro.CommandLine.Tui.Input;
using ChilliCream.Nitro.CommandLine.Tui.Mail;
using ChilliCream.Nitro.CommandLine.Tui.Memory;
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
        var tasksTab = new TuiTab("Tasks", mnemonic: 'T', boardMode, new KeyDispatcher(KeyMap.CreateDefaultGlobal()));

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
    public void BuildTabs_Should_RegisterTabsInOrder_TasksMailAgentsMemory()
    {
        // arrange: guards against the tab order regressing now that a
        // fourth tab exists (perles-net-w27 flagged the absence of this
        // assertion as a finding for slice g to close).
        var taskStore = new FakeTaskStore();
        var mailStore = new FakeMailStore();
        var agentRegistry = new ChilliCream.Nitro.CommandLine.Tests.Tui.Agents.FakeAgentRegistry();
        var timeProvider = new FakeTimeProvider(Now);
        var environment = CreateEnvironment(mailActor: "alice").Object;

        var tempRoot = Directory.CreateTempSubdirectory("nitro-agent-tui-launcher-tests");

        try
        {
            var workingDirectory = Path.Combine(tempRoot.FullName, "acme");
            Directory.CreateDirectory(workingDirectory);
            var globalMemoryDirectory = Path.Combine(tempRoot.FullName, "app-data", "nitro", "memory");

            var memoryStore = new MemoryStore(
                new ChilliCream.Nitro.CommandLine.Tests.Agents.TestFileSystem(workingDirectory),
                timeProvider,
                globalMemoryDirectory);

            var tabs = AgentTuiLauncher.BuildTabs(
                taskStore, mailStore, memoryStore, agentRegistry, timeProvider, environment);

            var shell = new TuiShell(
                tabs,
                80,
                24,
                tasksTabIndex: 0,
                new SearchMode(taskStore),
                new DependencyTreeView(taskStore, rootId: ""),
                taskStore,
                actor: "tasks-actor");

            // act
            var text = RenderToText(shell);

            // assert: order matters here, not just presence, since
            // Assert.Contains alone would not catch the tabs being reordered.
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
        Assert.Contains("[T]asks", text);
        Assert.Contains("[M]ail", text);
    }

    [Fact]
    public void Shell_Should_StillOpenOnTheTasksTab_When_TheMemoryStoreThrows()
    {
        // arrange: a curated markdown file with malformed frontmatter
        // written directly into the global memory store, the only way such
        // a file reaches disk (MemoryStore itself never writes an
        // unparsable one). OnEnter's read of it must not let the resulting
        // ExitException escape shell construction.
        var tempRoot = Directory.CreateTempSubdirectory("nitro-agent-tui-launcher-memory-tests");

        try
        {
            var workingDirectory = Path.Combine(tempRoot.FullName, "acme");
            Directory.CreateDirectory(workingDirectory);
            var applicationDataDirectory = Path.Combine(tempRoot.FullName, "app-data");
            var globalMemoryDirectory = AgentWorkspace.GetGlobalMemoryDirectory(applicationDataDirectory);
            var globalCuratedDirectory = AgentWorkspace.GetMemoryCuratedDirectory(globalMemoryDirectory);
            Directory.CreateDirectory(globalCuratedDirectory);
            File.WriteAllText(Path.Combine(globalCuratedDirectory, "mem-broken.md"), "not frontmatter at all");

            var taskStore = new FakeTaskStore();
            var timeProvider = new FakeTimeProvider(Now);

            var memoryStore = new MemoryStore(
                new ChilliCream.Nitro.CommandLine.Tests.Agents.TestFileSystem(workingDirectory),
                timeProvider,
                globalMemoryDirectory);

            var tabs = AgentTuiLauncher.BuildTabs(
                taskStore,
                new FakeMailStore(),
                memoryStore,
                new ChilliCream.Nitro.CommandLine.Tests.Tui.Agents.FakeAgentRegistry(),
                timeProvider,
                CreateEnvironment(mailActor: "alice").Object);
            var tasksTab = tabs[0];
            var memoryTab = tabs[3];

            // act: construction alone must not throw despite the memory
            // tab's store read failure.
            var shell = new TuiShell(
                [tasksTab, memoryTab],
                80,
                24,
                tasksTabIndex: 0,
                new SearchMode(taskStore),
                new DependencyTreeView(taskStore, rootId: ""),
                taskStore,
                actor: "tasks-actor");
            var text = RenderToText(shell);

            // assert: the shell opened and both tabs render.
            Assert.Contains("[T]asks", text);
            Assert.Contains("M[e]mory", text);
        }
        finally
        {
            tempRoot.Delete(recursive: true);
        }
    }
}
