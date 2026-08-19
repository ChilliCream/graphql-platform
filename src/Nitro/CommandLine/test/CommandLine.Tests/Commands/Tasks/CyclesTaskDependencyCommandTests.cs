
namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Tasks;

public sealed class CyclesTaskDependencyCommandTests(NitroCommandFixture fixture)
    : TasksCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("agent", "tasks", "dep", "cycles", "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Detect cycles among blocking dependencies.

            Usage:
              nitro agent tasks dep cycles [options]

            Options:
              --output <json>  The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help   Show help and usage information

            Example:
              nitro agent tasks dep cycles
            """);
    }

    [Fact]
    public async Task NoDependencies_PrintsNoCyclesFound()
    {
        // arrange
        await InitWorkspaceAsync();

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "dep", "cycles");

        // assert
        result.AssertSuccess("No cycles found.");
    }

    [Fact]
    public async Task TwoNodeBlockingCycle_IsReported()
    {
        // arrange
        await InitWorkspaceAsync();
        var a = await CreateTaskAsync("Task A");
        var b = await CreateTaskAsync("Task B");

        // task dep add now rejects a cycle before commit, so the edges are
        // seeded directly, as if they reached the database another way.
        await InsertDependencyAsync(a, b);
        await InsertDependencyAsync(b, a);

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "dep", "cycles");

        // assert
        var smaller = string.CompareOrdinal(a, b) < 0 ? a : b;
        var larger = smaller == a ? b : a;
        result.AssertSuccess($"{smaller} -> {larger} -> {smaller}");
    }

    [Fact]
    public async Task NonBlockingLoop_IsNotReported()
    {
        // arrange
        await InitWorkspaceAsync();
        var a = await CreateTaskAsync("Task A");
        var b = await CreateTaskAsync("Task B");
        await ExecuteCommandAsync("agent", "tasks", "dep", "add", a, b, "--type", "related");
        await ExecuteCommandAsync("agent", "tasks", "dep", "add", b, a, "--type", "related");

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "dep", "cycles");

        // assert
        result.AssertSuccess("No cycles found.");
    }

    [Fact]
    public async Task JsonOutput_NoCycles_ReturnsEmptyItems()
    {
        // arrange
        await InitWorkspaceAsync();
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "dep", "cycles");

        // assert
        result.AssertSuccess(
            """
            {
              "items": []
            }
            """);
    }

    [Fact]
    public async Task JsonOutput_TwoNodeBlockingCycle_ReturnsStructuredCycle()
    {
        // arrange
        await InitWorkspaceAsync();
        var a = await CreateTaskAsync("Task A");
        var b = await CreateTaskAsync("Task B");

        // task dep add now rejects a cycle before commit, so the edges are
        // seeded directly, as if they reached the database another way.
        await InsertDependencyAsync(a, b);
        await InsertDependencyAsync(b, a);
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "dep", "cycles");

        // assert
        var smaller = string.CompareOrdinal(a, b) < 0 ? a : b;
        var larger = smaller == a ? b : a;
        result.AssertSuccess(
            $$"""
            {
              "items": [
                {
                  "tasks": [
                    "{{smaller}}",
                    "{{larger}}"
                  ]
                }
              ]
            }
            """);
    }

    [Fact]
    public async Task ThreeNodeBlockingCycle_IsReportedOnce()
    {
        // arrange
        await InitWorkspaceAsync();
        var a = await CreateTaskAsync("Task A");
        var b = await CreateTaskAsync("Task B");
        var c = await CreateTaskAsync("Task C");

        // task dep add now rejects a cycle before commit, so the edges are
        // seeded directly, as if they reached the database another way.
        await InsertDependencyAsync(a, b);
        await InsertDependencyAsync(b, c);
        await InsertDependencyAsync(c, a);

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "dep", "cycles");

        // assert
        // The cycle is the fixed chain a -> b -> c -> a; rotate that circular
        // sequence to start at whichever ID sorts smallest, matching how the
        // command always starts a cycle at its smallest member.
        var cycle = new List<string> { a, b, c };
        var startIndex = cycle.IndexOf(cycle.OrderBy(x => x, StringComparer.Ordinal).First());
        var rotated = cycle.Skip(startIndex).Concat(cycle.Take(startIndex)).ToList();
        result.AssertSuccess(string.Join(" -> ", rotated) + $" -> {rotated[0]}");
    }
}
