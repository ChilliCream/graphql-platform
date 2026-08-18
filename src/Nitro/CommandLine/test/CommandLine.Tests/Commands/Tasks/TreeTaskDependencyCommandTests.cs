
namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Tasks;

public sealed class TreeTaskDependencyCommandTests(NitroCommandFixture fixture)
    : TasksCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("task", "dep", "tree", "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Show a task's outgoing dependency tree.

            Usage:
              nitro task dep tree <id> [options]

            Arguments:
              <id>  The task ID

            Options:
              -?, -h, --help  Show help and usage information

            Example:
              nitro task dep tree "acme-1a2"
            """);
    }

    [Fact]
    public async Task NoDependencies_PrintsRootOnly()
    {
        // arrange
        await InitWorkspaceAsync();
        var id = await CreateTaskAsync("Fix the parser");

        // act
        var result = await ExecuteCommandAsync("task", "dep", "tree", id);

        // assert
        result.AssertSuccess($"{id} (open) Fix the parser");
    }

    [Fact]
    public async Task Chain_PrintsNestedIndentation()
    {
        // arrange
        await InitWorkspaceAsync();
        var root = await CreateTaskAsync("Root task");
        var child = await CreateTaskAsync("Child task");
        var grandchild = await CreateTaskAsync("Grandchild task");
        await ExecuteCommandAsync("task", "dep", "add", root, child);
        await ExecuteCommandAsync("task", "dep", "add", child, grandchild);

        // act
        var result = await ExecuteCommandAsync("task", "dep", "tree", root);

        // assert
        result.AssertSuccess(
            $"""
            {root} (open) Root task
              {child} (blocks, open) Child task
                {grandchild} (blocks, open) Grandchild task
            """);
    }

    [Fact]
    public async Task RepeatedNode_MarkedWithAsteriskAndNotExpanded()
    {
        // arrange
        await InitWorkspaceAsync();
        var root = await CreateTaskAsync("Root task");
        var left = await CreateTaskAsync("Left branch");
        var right = await CreateTaskAsync("Right branch");
        var shared = await CreateTaskAsync("Shared dependency");
        await ExecuteCommandAsync("task", "dep", "add", root, left);
        await ExecuteCommandAsync("task", "dep", "add", root, right);
        await ExecuteCommandAsync("task", "dep", "add", left, shared);
        await ExecuteCommandAsync("task", "dep", "add", right, shared);

        // act
        var result = await ExecuteCommandAsync("task", "dep", "tree", root);

        // assert
        // Children of the root are sorted by (type, depends-on-id); both edges
        // are type "blocks", so the ordinally smaller ID comes first.
        var firstIsLeft = string.CompareOrdinal(left, right) < 0;
        var first = firstIsLeft ? left : right;
        var firstTitle = firstIsLeft ? "Left branch" : "Right branch";
        var second = firstIsLeft ? right : left;
        var secondTitle = firstIsLeft ? "Right branch" : "Left branch";

        result.AssertSuccess(
            $"""
            {root} (open) Root task
              {first} (blocks, open) {firstTitle}
                {shared} (blocks, open) Shared dependency
              {second} (blocks, open) {secondTitle}
                {shared} (blocks, open) Shared dependency *
            """);
    }

    [Fact]
    public async Task TaskNotFound_ReturnsError()
    {
        // arrange
        await InitWorkspaceAsync();

        // act
        var result = await ExecuteCommandAsync("task", "dep", "tree", "acme-999");

        // assert
        result.AssertError("Task 'acme-999' does not exist.");
    }
}
