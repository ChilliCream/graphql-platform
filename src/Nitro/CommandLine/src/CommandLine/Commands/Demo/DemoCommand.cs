using ChilliCream.Nitro.CommandLine.Helpers;

namespace ChilliCream.Nitro.CommandLine.Commands.Demo;

internal sealed class DemoCommand : Command
{
    private static readonly Option<string?> _scenarioOption = new("--scenario")
    {
        Description = "Which scenario to run: happy, crash, late-update, long-lines, siblings, stderr."
    };

    public DemoCommand() : base("demo")
    {
        Description = "Demo command to exercise all activity features.";

        Options.Add(_scenarioOption);

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static async Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken ct)
    {
        var console = services.GetRequiredService<INitroConsole>();
        var scenario = parseResult.GetValue(_scenarioOption) ?? "happy";

        return scenario switch
        {
            "happy" => await RunHappyPathAsync(console, ct),
            "crash" => await RunCrashAsync(console, ct),
            "late-update" => await RunLateUpdateAsync(console, ct),
            "long-lines" => await RunLongLinesAsync(console, ct),
            "siblings" => await RunSiblingsAsync(console, ct),
            "stderr" => await RunStderrAsync(console, ct),
            _ => throw new ExitException($"Unknown scenario '{scenario}'.")
        };
    }

    // Scenario: happy — everything succeeds normally
    private static async Task<int> RunHappyPathAsync(INitroConsole console, CancellationToken ct)
    {
        await using (var root = console.StartActivity(
            "Publishing new schema version 'v2' of API 'my-api' to stage 'production'",
            "Failed to publish schema version."))
        {
            root.Update("Force push is enabled.", ActivityUpdateKind.Warning);
            await Task.Delay(1000, ct);

            root.Update("Publication request created. (ID: req-abc123)");
            await Task.Delay(1000, ct);

            root.Update("Your request is queued at position 3.", ActivityUpdateKind.Waiting);
            await Task.Delay(1000, ct);

            await using (var child1 = root.StartChildActivity(
                "Validating schema",
                "Schema validation failed."))
            {
                child1.Update("Checking for breaking changes...");
                await Task.Delay(1000, ct);

                var resultTree = new Tree("[bold]Validation Results[/]");
                var operations = resultTree.AddNode("[green]Operations[/] [dim](3 checked)[/]");
                operations.AddNode("[dim]Query.users[/] — [green]compatible[/]");
                operations.AddNode("[dim]Query.products[/] — [green]compatible[/]");
                operations.AddNode("[dim]Mutation.createOrder[/] — [green]compatible[/]");
                var types = resultTree.AddNode("[green]Types[/] [dim](2 checked)[/]");
                types.AddNode("[dim]User[/] — [green]no breaking changes[/]");
                types.AddNode("[dim]Product[/] — [yellow]1 deprecated field[/]");

                child1.Update("Validated 3 operations and 2 types.", details: resultTree);
                await Task.Delay(1000, ct);

                child1.Success("Schema validation successful.");
            }

            await Task.Delay(1000, ct);

            await using (var child2 = root.StartChildActivity(
                "Deploying to stage",
                "Deployment failed."))
            {
                child2.Update("Waiting for approval.", ActivityUpdateKind.Waiting);
                await Task.Delay(1000, ct);

                await using (var grandchild = child2.StartChildActivity(
                    "Rolling out to instances",
                    "Rollout failed."))
                {
                    grandchild.Update("Updating 3 instances...");
                    await Task.Delay(1000, ct);

                    grandchild.Success("All instances updated.");
                }

                child2.Success("Deployed to stage 'production'.");
            }

            await Task.Delay(1000, ct);

            root.Success("Published new schema version 'v2' of API 'my-api' to stage 'production'.");
        }

        return ExitCodes.Success;
    }

    // Scenario: crash — exception thrown inside a child activity
    // Tests: FailAllAsync propagation, FailSilent on parent, dispose cleanup
    private static async Task<int> RunCrashAsync(INitroConsole console, CancellationToken ct)
    {
        await using (var root = console.StartActivity(
            "Publishing schema",
            "Publishing failed."))
        {
            await using (var child1 = root.StartChildActivity(
                "Validating schema",
                "Validation failed."))
            {
                child1.Update("Checking for breaking changes...");
                await Task.Delay(1000, ct);

                child1.Success("Schema validation successful.");
            }

            await Task.Delay(500, ct);

            await using (var child2 = root.StartChildActivity(
                "Deploying to stage",
                "Deployment failed."))
            {
                child2.Update("Starting deployment...");
                await Task.Delay(1000, ct);

                throw new InvalidOperationException(
                    "Simulated crash while deploying.");
            }
        }

        // Unreachable — the throw above always propagates out.
        // Kept to satisfy the return type; the exception handler in
        // CommandExtensions will catch it before we get here.
#pragma warning disable CS0162
        return ExitCodes.Success;
#pragma warning restore CS0162
    }

    // Scenario: late-update — Update() called after Success()
    // Tests: whether _completed guard exists on Update
    private static async Task<int> RunLateUpdateAsync(INitroConsole console, CancellationToken ct)
    {
        await using (var root = console.StartActivity(
            "Processing items",
            "Processing failed."))
        {
            await using (var child = root.StartChildActivity(
                "Uploading files",
                "Upload failed."))
            {
                child.Update("Uploading file 1 of 3...");
                await Task.Delay(800, ct);

                child.Success("All files uploaded.");

                // These arrive after Success — should be ignored but aren't
                await Task.Delay(200, ct);
                child.Update("Uploading file 2 of 3...");
                child.Update("Uploading file 3 of 3...");
            }

            // Also on root
            root.Success("All items processed.");
            await Task.Delay(200, ct);
            root.Update("Late event on root — should not appear.");
        }

        return ExitCodes.Success;
    }

    // Scenario: long-lines — text that wraps past the tree guides
    // Tests: whether continuation lines align under the correct branch
    private static async Task<int> RunLongLinesAsync(INitroConsole console, CancellationToken ct)
    {
        await using (var root = console.StartActivity(
            "Publishing schema version 'v42-beta.3+build.20240601' of API 'my-very-long-api-name-that-will-definitely-exceed-most-terminal-widths' to stage 'production-us-east-1-primary'",
            "Failed to publish."))
        {
            await Task.Delay(500, ct);

            await using (var child = root.StartChildActivity(
                "Validating schema compatibility with all downstream consumers including service-a, service-b, service-c, service-d, and service-e across all active deployment stages",
                "Validation failed."))
            {
                child.Update(
                    "Checked 142 persisted operations across 12 client applications. "
                    + "Found 0 breaking changes, 3 deprecated field usages in client-web-app-v2, "
                    + "and 1 advisory notice for field 'User.legacyId' scheduled for removal in Q3 2025.");
                await Task.Delay(1000, ct);

                child.Success(
                    "Schema is compatible with all 12 consumers. "
                    + "3 deprecation warnings issued for client-web-app-v2. "
                    + "See full compatibility report at https://nitro.example.com/reports/compat/req-abc123.");
            }

            await Task.Delay(500, ct);

            root.Success("Published successfully.");
        }

        return ExitCodes.Success;
    }

    // Scenario: siblings — two children active, one fails
    // Tests: whether the surviving sibling gets stuck with a frozen spinner
    private static async Task<int> RunSiblingsAsync(INitroConsole console, CancellationToken ct)
    {
        await using (var root = console.StartActivity(
            "Deploying configuration",
            "Deployment failed."))
        {
            // Start two children — simulate parallel work by not awaiting using on the first
            var child1 = root.StartChildActivity(
                "Uploading to region us-east-1",
                "Upload to us-east-1 failed.");

            await using (var child2 = root.StartChildActivity(
                "Uploading to region eu-west-1",
                "Upload to eu-west-1 failed."))
            {
                child2.Update("Uploading...");
                await Task.Delay(1000, ct);

                // child1 is still Active — never completed
                // child2 throws, triggering FailAllAsync
                throw new InvalidOperationException(
                    "Simulated failure in eu-west-1 upload.");
            }

            // child1 is never disposed explicitly — it stays Active
        }

#pragma warning disable CS0162
        return ExitCodes.Success;
#pragma warning restore CS0162
    }

    // Scenario: stderr — error written to console.Error while live display is still active
    // Tests: whether stderr output interleaves with the live renderer
    private static async Task<int> RunStderrAsync(INitroConsole console, CancellationToken ct)
    {
        await using (var root = console.StartActivity(
            "Deploying configuration",
            "Deployment failed."))
        {
            await using (var child = root.StartChildActivity(
                "Validating configuration",
                "Validation failed."))
            {
                child.Update("Running validation checks...");
                await Task.Delay(1000, ct);

                child.Fail("Validation returned errors.");
            }

            // Write to stderr while the live display is still active
            // (root hasn't been disposed yet, so the live renderer is running)
            console.Error.WriteErrorLine(
                "ERROR: Configuration validation failed. "
                + "Please check your configuration and try again.");
            console.Error.WriteErrorLine(
                "Hint: Run 'nitro config validate --verbose' for detailed diagnostics.");

            await Task.Delay(500, ct);

            root.Fail("Deployment aborted due to validation errors.");
        }

        return ExitCodes.Success;
    }
}
