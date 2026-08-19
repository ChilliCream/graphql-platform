using System.Text;
using System.Text.Json;
using ChilliCream.Nitro.CommandLine.Commands.Tasks.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Tasks;

namespace ChilliCream.Nitro.CommandLine.Commands.Tasks;

internal sealed class SyncTaskCommand : Command
{
    public SyncTaskCommand() : base("sync")
    {
        Description = "Synchronize the task database with the committed tasks.jsonl file.";

        Options.Add(Opt<SyncFlushOnlyOption>.Instance);
        Options.Add(Opt<SyncImportOnlyOption>.Instance);
        Options.Add(Opt<SyncStatusOption>.Instance);

        Validators.Add(result =>
        {
            var selected = 0;
            selected += result.GetValue(Opt<SyncFlushOnlyOption>.Instance) ? 1 : 0;
            selected += result.GetValue(Opt<SyncImportOnlyOption>.Instance) ? 1 : 0;
            selected += result.GetValue(Opt<SyncStatusOption>.Instance) ? 1 : 0;

            if (selected != 1)
            {
                result.AddError(
                    "Specify exactly one of '--flush-only', '--import-only', or '--status'.");
            }
        });

        this.AddExamples(
            "task sync --flush-only",
            "task sync --import-only",
            "task sync --status");

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static async Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken cancellationToken)
    {
        var console = services.GetRequiredService<INitroConsole>();
        var store = services.GetRequiredService<ITaskStore>();
        var fileSystem = services.GetRequiredService<IFileSystem>();

        if (parseResult.GetValue(Opt<SyncFlushOnlyOption>.Instance))
        {
            return await FlushAsync(console, store, fileSystem, cancellationToken);
        }

        if (parseResult.GetValue(Opt<SyncImportOnlyOption>.Instance))
        {
            return await ImportAsync(console, store, fileSystem, cancellationToken);
        }

        return await ReportStatusAsync(console, store, fileSystem, cancellationToken);
    }

    private static async Task<int> FlushAsync(
        INitroConsole console,
        ITaskStore store,
        IFileSystem fileSystem,
        CancellationToken cancellationToken)
    {
        var workspaceDirectory = store.FindWorkspaceDirectory()
            ?? throw new ExitException(
                "No task workspace found. Run `nitro task init` first.");

        var config = await store.ListConfigAsync(cancellationToken);
        var records = await store.ExportTasksAsync(cancellationToken);

        await fileSystem.WriteAllTextAsync(
            TaskWorkspace.GetJsonlPath(workspaceDirectory),
            SerializeJsonl(config, records),
            cancellationToken);

        console.OkLine(
            $"Flushed {records.Count} {(records.Count == 1 ? "task" : "tasks")} to "
            + $"'{TaskWorkspace.DisplayPath}/{TaskWorkspace.JsonlFileName}'.");

        return ExitCodes.Success;
    }

    private static async Task<int> ImportAsync(
        INitroConsole console,
        ITaskStore store,
        IFileSystem fileSystem,
        CancellationToken cancellationToken)
    {
        var workspaceDirectory = store.FindWorkspaceDirectory()
            ?? TaskWorkspace.FindDatabaseOrJsonl(fileSystem, fileSystem.GetCurrentDirectory())
            ?? throw new ExitException(
                "No task workspace found. Run `nitro task init` first.");

        var jsonlPath = TaskWorkspace.GetJsonlPath(workspaceDirectory);

        if (!fileSystem.FileExists(jsonlPath))
        {
            throw new ExitException(
                $"No '{TaskWorkspace.JsonlFileName}' found at "
                + $"'{TaskWorkspace.DisplayPath}/{TaskWorkspace.JsonlFileName}'.");
        }

        var (config, records) = await ReadJsonlAsync(fileSystem, jsonlPath, cancellationToken);

        await store.EnsureWorkspaceAsync(workspaceDirectory, cancellationToken);

        foreach (var entry in config)
        {
            await store.SetConfigAsync(entry.Key, entry.Value, cancellationToken);
        }

        var result = await store.ImportTasksAsync(records, cancellationToken);

        console.OkLine(
            $"Imported {result.Applied} of {result.Total} "
            + $"{(result.Total == 1 ? "task" : "tasks")} "
            + $"({result.Skipped} already current).");

        return ExitCodes.Success;
    }

    private static async Task<int> ReportStatusAsync(
        INitroConsole console,
        ITaskStore store,
        IFileSystem fileSystem,
        CancellationToken cancellationToken)
    {
        var workspaceDirectory = store.FindWorkspaceDirectory()
            ?? TaskWorkspace.FindDatabaseOrJsonl(fileSystem, fileSystem.GetCurrentDirectory())
            ?? throw new ExitException(
                "No task workspace found. Run `nitro task init` first.");

        var jsonlPath = TaskWorkspace.GetJsonlPath(workspaceDirectory);
        var hasDatabase = fileSystem.FileExists(TaskWorkspace.GetDatabasePath(workspaceDirectory));
        var hasJsonl = fileSystem.FileExists(jsonlPath);

        if (!hasDatabase)
        {
            console.WriteLine(
                "No task database. Run `nitro task sync --import-only` to create it from "
                + $"'{TaskWorkspace.JsonlFileName}'.");

            return ExitCodes.Error;
        }

        var config = await store.ListConfigAsync(cancellationToken);
        var records = await store.ExportTasksAsync(cancellationToken);

        if (!hasJsonl)
        {
            console.WriteLine(
                $"No '{TaskWorkspace.JsonlFileName}'. Run `nitro task sync --flush-only` to "
                + $"create it from {records.Count} "
                + $"{(records.Count == 1 ? "task" : "tasks")} in the database.");

            return ExitCodes.Error;
        }

        var expectedContent = SerializeJsonl(config, records);
        var actualContent = await fileSystem.ReadAllTextAsync(jsonlPath, cancellationToken);

        if (NormalizeLineEndings(expectedContent) == NormalizeLineEndings(actualContent))
        {
            console.OkLine(
                $"'{TaskWorkspace.JsonlFileName}' is in sync with the task database "
                + $"({records.Count} {(records.Count == 1 ? "task" : "tasks")}).");

            return ExitCodes.Success;
        }

        console.WriteLine(
            $"'{TaskWorkspace.JsonlFileName}' and the task database have diverged. Run "
            + "`nitro task sync --flush-only` or `nitro task sync --import-only` to reconcile.");

        return ExitCodes.Error;
    }

    /// <summary>
    /// Serializes one compact JSON object per line, newline-separated, so the
    /// result is valid JSONL: one workspace config entry per line, ordered by
    /// key, followed by one task per line.
    /// </summary>
    private static string SerializeJsonl(
        IReadOnlyList<TaskConfigEntry> config,
        IReadOnlyList<TaskSyncRecord> records)
    {
        var builder = new StringBuilder();

        foreach (var entry in config)
        {
            builder.Append(JsonSerializer.Serialize(entry, TaskSyncJsonContext.Default.TaskConfigEntry));
            builder.Append('\n');
        }

        foreach (var record in records)
        {
            builder.Append(JsonSerializer.Serialize(record, TaskSyncJsonContext.Default.TaskSyncRecord));
            builder.Append('\n');
        }

        return builder.ToString();
    }

    /// <summary>
    /// Parses tasks.jsonl, splitting its lines into workspace config entries
    /// and task records. A line is a config entry when it has no "id"
    /// property, which every task line carries; a config line instead
    /// carries "key" and "value".
    /// </summary>
    private static async Task<(IReadOnlyList<TaskConfigEntry> Config, IReadOnlyList<TaskSyncRecord> Records)>
        ReadJsonlAsync(
            IFileSystem fileSystem,
            string jsonlPath,
            CancellationToken cancellationToken)
    {
        var content = await fileSystem.ReadAllTextAsync(jsonlPath, cancellationToken);
        var config = new List<TaskConfigEntry>();
        var records = new List<TaskSyncRecord>();
        var lineNumber = 0;

        foreach (var line in content.ReplaceLineEndings("\n").Split('\n'))
        {
            lineNumber++;

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            try
            {
                using var document = JsonDocument.Parse(line);

                if (document.RootElement.TryGetProperty("id", out _))
                {
                    records.Add(
                        JsonSerializer.Deserialize(line, TaskSyncJsonContext.Default.TaskSyncRecord)
                        ?? throw new ExitException(
                            $"'{TaskWorkspace.JsonlFileName}' line {lineNumber} is empty."));
                }
                else
                {
                    config.Add(
                        JsonSerializer.Deserialize(line, TaskSyncJsonContext.Default.TaskConfigEntry)
                        ?? throw new ExitException(
                            $"'{TaskWorkspace.JsonlFileName}' line {lineNumber} is empty."));
                }
            }
            catch (JsonException exception)
            {
                throw new ExitException(
                    $"'{TaskWorkspace.JsonlFileName}' line {lineNumber} is not valid JSON: "
                    + exception.Message);
            }
        }

        return (config, records);
    }

    private static string NormalizeLineEndings(string value) => value.ReplaceLineEndings("\n");
}
