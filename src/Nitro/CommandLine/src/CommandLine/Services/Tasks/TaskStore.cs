using System.Data.Common;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using Dapper;
using Microsoft.Data.Sqlite;

[module: DapperAot]

namespace ChilliCream.Nitro.CommandLine.Services.Tasks;

internal sealed class TaskStore(IFileSystem fileSystem) : ITaskStore
{
    private const string PrefixConfigKey = "prefix";
    private const string IdAlphabet = "0123456789abcdefghijklmnopqrstuvwxyz";
    private const int MinIdLength = 3;
    private const int MaxIdAttempts = 10;

    static TaskStore() => SQLitePCL.Batteries_V2.Init();

    public async Task<SqliteConnection> InitializeAsync(
        string workspaceDirectory,
        CancellationToken cancellationToken)
    {
        var connection = await OpenAsync(
            TaskWorkspace.GetDatabasePath(workspaceDirectory),
            cancellationToken);

        await connection.ExecuteAsync(TaskStoreSchema.Create);
        await connection.ExecuteAsync(
            "PRAGMA user_version = " + TaskStoreSchema.CurrentVersion + ";");

        return connection;
    }

    public async Task<SqliteConnection> ConnectAsync(CancellationToken cancellationToken)
    {
        var workspaceDirectory = FindWorkspaceDirectory()
            ?? throw new ExitException(
                "No task workspace found. Run `nitro task init` first.");

        var connection = await OpenAsync(
            TaskWorkspace.GetDatabasePath(workspaceDirectory),
            cancellationToken);

        var version = await connection.ExecuteScalarAsync<long>("PRAGMA user_version;");

        if (version > TaskStoreSchema.CurrentVersion)
        {
            throw new ExitException(
                "The task workspace was created by a newer version of the Nitro CLI "
                + $"(schema v{version}, supported up to v{TaskStoreSchema.CurrentVersion}). "
                + "Update the CLI to use it.");
        }

        return connection;
    }

    public string? FindWorkspaceDirectory()
        => TaskWorkspace.Find(fileSystem, fileSystem.GetCurrentDirectory());

    public async Task<string?> GetConfigAsync(
        SqliteConnection connection,
        string key,
        CancellationToken cancellationToken,
        DbTransaction? transaction = null)
        => await connection.QueryFirstOrDefaultAsync<string>(
            "SELECT value FROM config WHERE key = @key",
            new { key, cancellationToken },
            transaction);

    public async Task SetConfigAsync(
        SqliteConnection connection,
        string key,
        string value,
        CancellationToken cancellationToken,
        DbTransaction? transaction = null)
        => await connection.ExecuteAsync(
            "INSERT INTO config (key, value) VALUES (@key, @value) "
            + "ON CONFLICT (key) DO UPDATE SET value = @value",
            new { key, value, cancellationToken },
            transaction);

    public async Task<string> GetPrefixAsync(
        SqliteConnection connection,
        CancellationToken cancellationToken,
        DbTransaction? transaction = null)
        => await GetConfigAsync(connection, PrefixConfigKey, cancellationToken, transaction)
            ?? TaskWorkspace.FallbackPrefix;

    public async Task<string> CreateTaskIdAsync(
        SqliteConnection connection,
        string? parentId,
        string seed,
        CancellationToken cancellationToken,
        DbTransaction? transaction = null)
    {
        if (parentId is not null)
        {
            var childNumber = await connection.ExecuteScalarAsync<long>(
                "INSERT INTO child_counters (parent_id, last_child) VALUES (@parentId, 1) "
                + "ON CONFLICT (parent_id) DO UPDATE SET last_child = last_child + 1 "
                + "RETURNING last_child",
                new { parentId, cancellationToken },
                transaction);

            return $"{parentId}.{childNumber}";
        }

        var prefix = await GetPrefixAsync(connection, cancellationToken, transaction);
        var taskCount = await connection.ExecuteScalarAsync<long>(
            "SELECT COUNT(*) FROM tasks",
            transaction: transaction);

        for (var attempt = 0; attempt < MaxIdAttempts; attempt++)
        {
            var id = $"{prefix}-{CreateIdSuffix(seed, taskCount, attempt)}";

            var exists = await connection.ExecuteScalarAsync<long>(
                "SELECT COUNT(*) FROM tasks WHERE id = @id",
                new { id, cancellationToken },
                transaction);

            if (exists == 0)
            {
                return id;
            }
        }

        throw new ExitException("Could not allocate a unique task ID.");
    }

    public async Task RecordEventAsync(
        SqliteConnection connection,
        TaskEvent taskEvent,
        CancellationToken cancellationToken,
        DbTransaction? transaction = null)
        => await connection.ExecuteAsync(
            "INSERT INTO events (task_id, event_type, actor, old_value, new_value, comment, created_at) "
            + "VALUES (@TaskId, @Type, @Actor, @OldValue, @NewValue, @Comment, @CreatedAt)",
            new
            {
                taskEvent.TaskId,
                taskEvent.Type,
                taskEvent.Actor,
                taskEvent.OldValue,
                taskEvent.NewValue,
                taskEvent.Comment,
                taskEvent.CreatedAt,
                cancellationToken
            },
            transaction);

    public async Task<TaskItem?> GetTaskAsync(
        SqliteConnection connection,
        string id,
        CancellationToken cancellationToken,
        DbTransaction? transaction = null)
    {
        // The intercepted read path cannot convert the TEXT-stored timestamp
        // columns to DateTimeOffset, so this materializes an all-primitives
        // row and parses the timestamps itself.
        var row = await connection.QueryFirstOrDefaultAsync<TaskRow>(
            $"SELECT {TaskItem.Columns} FROM tasks WHERE id = @id",
            new { id },
            transaction);

        return row?.ToTaskItem();
    }

    public async Task<TaskItem> GetRequiredTaskAsync(
        SqliteConnection connection,
        string id,
        CancellationToken cancellationToken,
        DbTransaction? transaction = null)
    {
        var task = await GetTaskAsync(connection, id, cancellationToken, transaction);

        if (task is null || task.Status == TaskStates.Tombstone)
        {
            throw new ExitException($"Task '{id}' does not exist.");
        }

        return task;
    }

    public async Task<IReadOnlyDictionary<string, IReadOnlyList<string>>> ComputeBlockedAsync(
        SqliteConnection connection,
        CancellationToken cancellationToken)
    {
        var tasks = (await connection.QueryAsync<TaskGraphNode>(
            "SELECT id AS Id, status AS Status, task_type AS Type FROM tasks"))
            .ToDictionary(t => t.Id);

        var dependencies = (await connection.QueryAsync<TaskGraphEdge>(
            "SELECT task_id AS TaskId, depends_on_id AS DependsOnId, dependency_type AS Type "
            + "FROM dependencies"))
            .ToList();

        var blocked = new Dictionary<string, List<string>>();

        // Pass 1: tasks with a blocking dependency on a non-terminal or
        // missing target are blocked. Parent-child edges gate children only
        // through pass 2 (blocked parents); a merely open parent does not
        // block its children.
        foreach (var edge in dependencies)
        {
            if (edge.Type == TaskDependencyTypes.ParentChild
                || !TaskDependencyTypes.IsBlocking(edge.Type))
            {
                continue;
            }

            if (!tasks.ContainsKey(edge.TaskId))
            {
                continue;
            }

            if (!tasks.TryGetValue(edge.DependsOnId, out var target))
            {
                AddBlocker(blocked, edge.TaskId, $"{edge.DependsOnId}:unknown");
            }
            else if (!TaskStates.IsTerminal(target.Status))
            {
                AddBlocker(blocked, edge.TaskId, $"{edge.DependsOnId}:{target.Status}");
            }
        }

        // Pass 2: blocked parents propagate to their children, transitively.
        var childrenByParent = dependencies
            .Where(e => e.Type == TaskDependencyTypes.ParentChild)
            .GroupBy(e => e.DependsOnId)
            .ToDictionary(g => g.Key, g => g.Select(e => e.TaskId).ToList());

        var queue = new Queue<string>(blocked.Keys);
        var visited = new HashSet<string>(blocked.Keys);

        while (queue.TryDequeue(out var parentId))
        {
            if (!childrenByParent.TryGetValue(parentId, out var children))
            {
                continue;
            }

            foreach (var childId in children)
            {
                AddBlocker(blocked, childId, $"{parentId}:parent-blocked");

                if (visited.Add(childId))
                {
                    queue.Enqueue(childId);
                }
            }
        }

        // Pass 3: epics with non-terminal children are blocked. Runs after
        // pass 2 so a parent blocked only by its children does not re-block
        // those children.
        foreach (var edge in dependencies)
        {
            if (edge.Type != TaskDependencyTypes.ParentChild)
            {
                continue;
            }

            if (tasks.TryGetValue(edge.DependsOnId, out var parent)
                && parent.Type == TaskTypes.Epic
                && tasks.TryGetValue(edge.TaskId, out var child)
                && !TaskStates.IsTerminal(child.Status))
            {
                AddBlocker(blocked, edge.DependsOnId, $"{edge.TaskId}:child-open");
            }
        }

        return blocked.ToDictionary(
            pair => pair.Key,
            IReadOnlyList<string> (pair) =>
                [.. pair.Value.Order(StringComparer.Ordinal).Distinct()]);
    }

    // -------------------------------------------------------------------
    // New surface: backend-agnostic, no ADO.NET or SQLite types. Every
    // member below is a temporary stub; bd-oyf.2 and bd-oyf.3 replace them
    // with real implementations. No command calls this surface yet.
    // -------------------------------------------------------------------

    public Task<IReadOnlyList<TaskItem>> QueryTasksAsync(
        TaskFilter filter,
        CancellationToken cancellationToken)
        => throw NotImplemented();

    public Task<TaskItem?> GetTaskAsync(
        string id,
        CancellationToken cancellationToken)
        => throw NotImplemented();

    public Task<TaskItem> GetRequiredTaskAsync(
        string id,
        CancellationToken cancellationToken)
        => throw NotImplemented();

    public Task<IReadOnlyList<string>> GetLabelsAsync(
        string taskId,
        CancellationToken cancellationToken)
        => throw NotImplemented();

    public Task<IReadOnlyList<TaskLabelCount>> GetLabelCountsAsync(
        CancellationToken cancellationToken)
        => throw NotImplemented();

    public Task<IReadOnlyList<TaskComment>> GetCommentsAsync(
        string taskId,
        CancellationToken cancellationToken)
        => throw NotImplemented();

    public Task<IReadOnlyList<TaskDependencyDetail>> GetDependenciesAsync(
        string taskId,
        CancellationToken cancellationToken)
        => throw NotImplemented();

    public Task<IReadOnlyList<TaskDependentDetail>> GetDependentsAsync(
        string taskId,
        CancellationToken cancellationToken)
        => throw NotImplemented();

    public Task<IReadOnlyList<TaskDependency>> GetDependencyEdgesAsync(
        CancellationToken cancellationToken)
        => throw NotImplemented();

    public Task<IReadOnlyDictionary<string, IReadOnlyList<string>>> ComputeBlockedAsync(
        CancellationToken cancellationToken)
        => throw NotImplemented();

    public Task<IReadOnlyList<TaskEpicStatus>> GetEpicStatusesAsync(
        CancellationToken cancellationToken)
        => throw NotImplemented();

    public Task<int> CountTasksAsync(
        CancellationToken cancellationToken)
        => throw NotImplemented();

    public Task<IReadOnlyList<TaskCount>> CountTasksByAsync(
        TaskCountDimension dimension,
        CancellationToken cancellationToken)
        => throw NotImplemented();

    public Task<TaskStats> GetStatsAsync(
        CancellationToken cancellationToken)
        => throw NotImplemented();

    public Task<string?> GetConfigAsync(
        string key,
        CancellationToken cancellationToken)
        => throw NotImplemented();

    public Task SetConfigAsync(
        string key,
        string value,
        CancellationToken cancellationToken)
        => throw NotImplemented();

    public Task<IReadOnlyList<TaskConfigEntry>> ListConfigAsync(
        CancellationToken cancellationToken)
        => throw NotImplemented();

    public Task<string> GetPrefixAsync(
        CancellationToken cancellationToken)
        => throw NotImplemented();

    public Task InitializeWorkspaceAsync(
        string workspaceDirectory,
        string prefix,
        CancellationToken cancellationToken)
        => throw NotImplemented();

    public Task<TaskCreationResult> CreateTaskAsync(
        TaskCreation creation,
        CancellationToken cancellationToken)
        => throw NotImplemented();

    public Task<TaskUpdateResult> UpdateTaskAsync(
        string id,
        TaskUpdate update,
        CancellationToken cancellationToken)
        => throw NotImplemented();

    public Task<IReadOnlyList<TaskItem>> CloseTaskAsync(
        IReadOnlyList<string> ids,
        string reason,
        string actor,
        CancellationToken cancellationToken)
        => throw NotImplemented();

    public Task<TaskItem> ReopenTaskAsync(
        string id,
        string reason,
        string actor,
        CancellationToken cancellationToken)
        => throw NotImplemented();

    public Task<TaskItem> DeferTaskAsync(
        string id,
        DateTimeOffset until,
        string actor,
        CancellationToken cancellationToken)
        => throw NotImplemented();

    public Task<TaskItem> UndeferTaskAsync(
        string id,
        string actor,
        CancellationToken cancellationToken)
        => throw NotImplemented();

    public Task<TaskItem> DeleteTaskAsync(
        string id,
        string reason,
        string actor,
        CancellationToken cancellationToken)
        => throw NotImplemented();

    public Task<IReadOnlyList<TaskEpicStatus>> CloseEligibleEpicsAsync(
        string actor,
        CancellationToken cancellationToken)
        => throw NotImplemented();

    public Task<TaskComment> AddCommentAsync(
        string id,
        string text,
        string actor,
        CancellationToken cancellationToken)
        => throw NotImplemented();

    public Task<IReadOnlyList<TaskLabelChange>> AddLabelAsync(
        string id,
        IReadOnlyList<string> labels,
        string actor,
        CancellationToken cancellationToken)
        => throw NotImplemented();

    public Task RemoveLabelAsync(
        string id,
        string label,
        string actor,
        CancellationToken cancellationToken)
        => throw NotImplemented();

    public Task<TaskDependencyAddResult> AddDependencyAsync(
        string id,
        string dependsOnId,
        string type,
        string actor,
        CancellationToken cancellationToken)
        => throw NotImplemented();

    public Task RemoveDependencyAsync(
        string id,
        string dependsOnId,
        string actor,
        CancellationToken cancellationToken)
        => throw NotImplemented();

    private static NotSupportedException NotImplemented(
        [CallerMemberName] string memberName = "")
        => new(
            $"ITaskStore.{memberName} is not implemented yet. "
            + "No command calls the new backend-agnostic surface until the "
            + "read and write migration work lands.");

    private static void AddBlocker(
        Dictionary<string, List<string>> blocked,
        string taskId,
        string blocker)
    {
        if (!blocked.TryGetValue(taskId, out var blockers))
        {
            blockers = [];
            blocked[taskId] = blockers;
        }

        blockers.Add(blocker);
    }

    private static string CreateIdSuffix(string seed, long taskCount, int attempt)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes($"{seed}|{taskCount}|{attempt}"));
        var length = MinIdLength + attempt / 3;
        var suffix = new char[length];

        for (var i = 0; i < length; i++)
        {
            suffix[i] = IdAlphabet[hash[i] % IdAlphabet.Length];
        }

        return new string(suffix);
    }

    private static async Task<SqliteConnection> OpenAsync(
        string databasePath,
        CancellationToken cancellationToken)
    {
        // Pooling would keep the database file open after the connection is
        // disposed; a CLI process runs one command and exits, so it gains
        // nothing from the pool.
        var connection = new SqliteConnection($"Data Source={databasePath};Pooling=False");

        await connection.OpenAsync(cancellationToken);
        await connection.ExecuteAsync(
            "PRAGMA journal_mode = WAL; PRAGMA foreign_keys = ON;");

        return connection;
    }

    private sealed class TaskRow
    {
        public required string Id { get; init; }
        public required string Title { get; init; }
        public string Description { get; init; } = "";
        public string Design { get; init; } = "";
        public string AcceptanceCriteria { get; init; } = "";
        public string Notes { get; init; } = "";
        public string Status { get; init; } = TaskStates.Open;
        public int Priority { get; init; }
        public string Type { get; init; } = TaskTypes.Task;
        public string? Assignee { get; init; }
        public int? EstimatedMinutes { get; init; }
        public string? DueAt { get; init; }
        public string? DeferUntil { get; init; }
        public required string CreatedAt { get; init; }
        public string CreatedBy { get; init; } = "";
        public required string UpdatedAt { get; init; }
        public string? ClosedAt { get; init; }
        public string CloseReason { get; init; } = "";
        public string? DeletedAt { get; init; }
        public string DeleteReason { get; init; } = "";

        public TaskItem ToTaskItem() => new()
        {
            Id = Id,
            Title = Title,
            Description = Description,
            Design = Design,
            AcceptanceCriteria = AcceptanceCriteria,
            Notes = Notes,
            Status = Status,
            Priority = Priority,
            Type = Type,
            Assignee = Assignee,
            EstimatedMinutes = EstimatedMinutes,
            DueAt = ParseDate(DueAt),
            DeferUntil = ParseDate(DeferUntil),
            CreatedAt = ParseDate(CreatedAt)!.Value,
            CreatedBy = CreatedBy,
            UpdatedAt = ParseDate(UpdatedAt)!.Value,
            ClosedAt = ParseDate(ClosedAt),
            CloseReason = CloseReason,
            DeletedAt = ParseDate(DeletedAt),
            DeleteReason = DeleteReason
        };

        private static DateTimeOffset? ParseDate(string? value)
            => value is null
                ? null
                : DateTimeOffset.Parse(value, CultureInfo.InvariantCulture);
    }

    private sealed class TaskGraphNode
    {
        public required string Id { get; init; }
        public required string Status { get; init; }
        public required string Type { get; init; }
    }

    private sealed class TaskGraphEdge
    {
        public required string TaskId { get; init; }
        public required string DependsOnId { get; init; }
        public required string Type { get; init; }
    }
}
