using System.Data.Common;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using Dapper;
using Microsoft.Data.Sqlite;

[module: DapperAot]

namespace ChilliCream.Nitro.CommandLine.Services.Tasks;

internal sealed class TaskStore(IFileSystem fileSystem, TimeProvider timeProvider) : ITaskStore
{
    private const string PrefixConfigKey = "prefix";
    private const string IdAlphabet = "0123456789abcdefghijklmnopqrstuvwxyz";
    private const int MinIdLength = 3;
    private const int MaxIdAttempts = 10;

    private static readonly string[] StatusOrder =
    [
        TaskStates.Open,
        TaskStates.InProgress,
        TaskStates.Blocked,
        TaskStates.Deferred,
        TaskStates.Closed,
        TaskStates.Tombstone
    ];

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
    // New surface: backend-agnostic, no ADO.NET or SQLite types. The read
    // members below (bd-oyf.2) are real implementations; the write members
    // further down (bd-oyf.3) are still stubs. No command calls this
    // surface yet.
    // -------------------------------------------------------------------

    public async Task<IReadOnlyList<TaskItem>> QueryTasksAsync(
        TaskFilter filter,
        CancellationToken cancellationToken)
    {
        var (whereClause, parameters) = BuildTaskFilterClause(filter);
        var orderBy = OrderByClause(filter.Ordering);

        var sql = $"SELECT {TaskItem.Columns} FROM tasks{whereClause}{orderBy}";

        if (!filter.ExcludeBlocked && filter.Limit is { } sqlLimit)
        {
            parameters.Add("limit", sqlLimit);
            sql += " LIMIT @limit";
        }

        await using var connection = await ConnectAsync(cancellationToken);

        if (!filter.ExcludeBlocked)
        {
            return (await connection.QueryAsync<TaskItem>(
                new CommandDefinition(sql, parameters, cancellationToken: cancellationToken)))
                .ToList();
        }

        var blocked = await ComputeBlockedAsync(connection, cancellationToken);

        IEnumerable<TaskItem> tasks = await connection.QueryAsync<TaskItem>(
            new CommandDefinition(sql, parameters, cancellationToken: cancellationToken));

        tasks = tasks.Where(t => !blocked.ContainsKey(t.Id));

        if (filter.Limit is { } limit)
        {
            tasks = tasks.Take(limit);
        }

        return tasks.ToList();
    }

    public async Task<TaskItem?> GetTaskAsync(
        string id,
        CancellationToken cancellationToken)
    {
        await using var connection = await ConnectAsync(cancellationToken);

        return await GetTaskAsync(connection, id, cancellationToken);
    }

    public async Task<TaskItem> GetRequiredTaskAsync(
        string id,
        CancellationToken cancellationToken)
    {
        await using var connection = await ConnectAsync(cancellationToken);

        return await GetRequiredTaskAsync(connection, id, cancellationToken);
    }

    public async Task<IReadOnlyList<string>> GetLabelsAsync(
        string taskId,
        CancellationToken cancellationToken)
    {
        await using var connection = await ConnectAsync(cancellationToken);

        return (await connection.QueryAsync<string>(
            "SELECT label FROM labels WHERE task_id = @taskId ORDER BY label",
            new { taskId, cancellationToken })).ToList();
    }

    public async Task<IReadOnlyList<TaskLabelCount>> GetLabelCountsAsync(
        CancellationToken cancellationToken)
    {
        await using var connection = await ConnectAsync(cancellationToken);

        // A row class, not the TaskLabelCount record, receives the COUNT(*)
        // column: Dapper's reflection path requires an exact constructor
        // match for record types, and SQLite's COUNT(*) always reads back as
        // Int64, not Int32. A settable property tolerates the narrowing.
        return (await connection.QueryAsync<LabelCountRow>(
                new CommandDefinition(
                    """
                    SELECT l.label AS Label, COUNT(*) AS Count
                    FROM labels l
                    JOIN tasks t ON t.id = l.task_id
                    WHERE t.status != @tombstoneStatus
                    GROUP BY l.label
                    ORDER BY l.label
                    """,
                    new { tombstoneStatus = TaskStates.Tombstone },
                    cancellationToken: cancellationToken)))
            .Select(r => new TaskLabelCount(r.Label, r.Count))
            .ToList();
    }

    public async Task<IReadOnlyList<TaskComment>> GetCommentsAsync(
        string taskId,
        CancellationToken cancellationToken)
    {
        await using var connection = await ConnectAsync(cancellationToken);

        // The intercepted read path cannot convert the TEXT-stored timestamp
        // column to DateTimeOffset, so this materializes an all-primitives
        // row and parses the timestamp itself.
        return (await connection.QueryAsync<TaskCommentRow>(
                $"SELECT {TaskComment.Columns} FROM comments WHERE task_id = @taskId "
                + "ORDER BY created_at, id",
                new { taskId, cancellationToken }))
            .Select(r => r.ToTaskComment())
            .ToList();
    }

    public async Task<IReadOnlyList<TaskDependencyDetail>> GetDependenciesAsync(
        string taskId,
        CancellationToken cancellationToken)
    {
        await using var connection = await ConnectAsync(cancellationToken);

        return (await connection.QueryAsync<TaskDependencyDetail>(
            """
            SELECT d.dependency_type AS Type, d.depends_on_id AS DependsOnId,
                   t.status AS Status, t.title AS Title
            FROM dependencies d
            LEFT JOIN tasks t ON t.id = d.depends_on_id
            WHERE d.task_id = @taskId
            ORDER BY d.created_at, d.depends_on_id
            """,
            new { taskId, cancellationToken })).ToList();
    }

    public async Task<IReadOnlyList<TaskDependentDetail>> GetDependentsAsync(
        string taskId,
        CancellationToken cancellationToken)
    {
        await using var connection = await ConnectAsync(cancellationToken);

        return (await connection.QueryAsync<TaskDependentDetail>(
            """
            SELECT d.task_id AS TaskId, d.dependency_type AS Type,
                   t.status AS Status, t.title AS Title
            FROM dependencies d
            LEFT JOIN tasks t ON t.id = d.task_id
            WHERE d.depends_on_id = @taskId
            ORDER BY d.created_at, d.task_id
            """,
            new { taskId, cancellationToken })).ToList();
    }

    public async Task<IReadOnlyList<TaskDependency>> GetDependencyEdgesAsync(
        CancellationToken cancellationToken)
    {
        await using var connection = await ConnectAsync(cancellationToken);

        // Uses the reflection (CommandDefinition) path so the created_at
        // column, stored as TEXT, converts to DateTimeOffset; the
        // intercepted classic shape cannot perform that conversion.
        return (await connection.QueryAsync<TaskDependency>(
            new CommandDefinition(
                $"SELECT {TaskDependency.Columns} FROM dependencies "
                + "ORDER BY task_id, depends_on_id",
                cancellationToken: cancellationToken))).ToList();
    }

    public async Task<IReadOnlyDictionary<string, IReadOnlyList<string>>> ComputeBlockedAsync(
        CancellationToken cancellationToken)
    {
        await using var connection = await ConnectAsync(cancellationToken);

        return await ComputeBlockedAsync(connection, cancellationToken);
    }

    public async Task<IReadOnlyList<TaskEpicStatus>> GetEpicStatusesAsync(
        CancellationToken cancellationToken)
    {
        await using var connection = await ConnectAsync(cancellationToken);

        return (await connection.QueryAsync<TaskEpicStatus>(
            new CommandDefinition(
                """
                SELECT e.id AS Id, e.title AS Title, e.status AS Status,
                       COUNT(c.id) AS Total,
                       SUM(CASE WHEN c.status = @closed THEN 1 ELSE 0 END) AS Closed
                FROM tasks e
                LEFT JOIN dependencies d
                    ON d.depends_on_id = e.id AND d.dependency_type = @parentChild
                LEFT JOIN tasks c
                    ON c.id = d.task_id AND c.status != @tombstone
                WHERE e.task_type = @epic AND e.status != @tombstone
                GROUP BY e.id, e.title, e.status
                ORDER BY e.id
                """,
                new
                {
                    closed = TaskStates.Closed,
                    parentChild = TaskDependencyTypes.ParentChild,
                    tombstone = TaskStates.Tombstone,
                    epic = TaskTypes.Epic
                },
                cancellationToken: cancellationToken))).ToList();
    }

    public async Task<int> CountTasksAsync(
        CancellationToken cancellationToken)
    {
        await using var connection = await ConnectAsync(cancellationToken);

        return await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM tasks WHERE status != @tombstone",
            new { tombstone = TaskStates.Tombstone, cancellationToken });
    }

    public async Task<IReadOnlyList<TaskCount>> CountTasksByAsync(
        TaskCountDimension dimension,
        CancellationToken cancellationToken)
    {
        await using var connection = await ConnectAsync(cancellationToken);

        // A row class, not the TaskCount record, receives the COUNT(*)
        // column in every branch: Dapper's reflection path requires an exact
        // constructor match for record types, and SQLite's COUNT(*) always
        // reads back as Int64, not Int32. A settable property tolerates the
        // narrowing.
        switch (dimension)
        {
            case TaskCountDimension.Status:
                return (await connection.QueryAsync<CountRow>(
                        new CommandDefinition(
                            "SELECT status AS Value, COUNT(*) AS Count FROM tasks "
                            + "WHERE status != @tombstone GROUP BY status ORDER BY status ASC",
                            new { tombstone = TaskStates.Tombstone },
                            cancellationToken: cancellationToken)))
                    .Select(r => new TaskCount(r.Value, r.Count))
                    .ToList();

            case TaskCountDimension.Type:
                return (await connection.QueryAsync<CountRow>(
                        new CommandDefinition(
                            "SELECT task_type AS Value, COUNT(*) AS Count FROM tasks "
                            + "WHERE status != @tombstone GROUP BY task_type ORDER BY task_type ASC",
                            new { tombstone = TaskStates.Tombstone },
                            cancellationToken: cancellationToken)))
                    .Select(r => new TaskCount(r.Value, r.Count))
                    .ToList();

            case TaskCountDimension.Priority:
                return (await connection.QueryAsync<PriorityCountRow>(
                        new CommandDefinition(
                            "SELECT priority AS Priority, COUNT(*) AS Count FROM tasks "
                            + "WHERE status != @tombstone GROUP BY priority ORDER BY priority ASC",
                            new { tombstone = TaskStates.Tombstone },
                            cancellationToken: cancellationToken)))
                    .Select(r => new TaskCount(TaskPriorities.Format(r.Priority), r.Count))
                    .ToList();

            case TaskCountDimension.Assignee:
                return (await connection.QueryAsync<CountRow>(
                        new CommandDefinition(
                            "SELECT COALESCE(NULLIF(assignee, ''), 'unassigned') AS Value, "
                            + "COUNT(*) AS Count FROM tasks WHERE status != @tombstone "
                            + "GROUP BY COALESCE(NULLIF(assignee, ''), 'unassigned') "
                            + "ORDER BY COALESCE(NULLIF(assignee, ''), 'unassigned') ASC",
                            new { tombstone = TaskStates.Tombstone },
                            cancellationToken: cancellationToken)))
                    .Select(r => new TaskCount(r.Value, r.Count))
                    .ToList();

            case TaskCountDimension.Label:
                return (await connection.QueryAsync<CountRow>(
                        new CommandDefinition(
                            "SELECT label AS Value, COUNT(*) AS Count FROM labels "
                            + "INNER JOIN tasks ON tasks.id = labels.task_id "
                            + "WHERE tasks.status != @tombstone GROUP BY label ORDER BY label ASC",
                            new { tombstone = TaskStates.Tombstone },
                            cancellationToken: cancellationToken)))
                    .Select(r => new TaskCount(r.Value, r.Count))
                    .ToList();

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(dimension), dimension, "Unknown task count dimension.");
        }
    }

    public async Task<TaskStats> GetStatsAsync(
        CancellationToken cancellationToken)
    {
        await using var connection = await ConnectAsync(cancellationToken);

        // A row class, not the TaskCount record, receives the COUNT(*)
        // column: Dapper's reflection path requires an exact constructor
        // match for record types, and SQLite's COUNT(*) always reads back as
        // Int64, not Int32. A settable property tolerates the narrowing.
        var statusCounts = (await connection.QueryAsync<CountRow>(
                new CommandDefinition(
                    "SELECT status AS Value, COUNT(*) AS Count FROM tasks "
                    + "WHERE status != @tombstone GROUP BY status",
                    new { tombstone = TaskStates.Tombstone },
                    cancellationToken: cancellationToken)))
            .Select(r => new TaskCount(r.Value, r.Count))
            .ToList();

        var now = timeProvider.GetUtcNow();
        var blocked = await ComputeBlockedAsync(connection, cancellationToken);

        var readyIds = await connection.QueryAsync<string>(
            "SELECT id FROM tasks WHERE status = @status "
            + "AND (defer_until IS NULL OR defer_until <= @now)",
            new { status = TaskStates.Open, now, cancellationToken });

        var readyCount = readyIds.Count(id => !blocked.ContainsKey(id));

        var blockedTaskStatuses = new Dictionary<string, string>();

        if (blocked.Count > 0)
        {
            // The IN clause needs the array-expansion that only the
            // reflection (CommandDefinition) path performs; the intercepted
            // classic shape sends "@ids" verbatim and SQLite rejects it.
            var rows = await connection.QueryAsync<TaskGraphNode>(
                new CommandDefinition(
                    "SELECT id AS Id, status AS Status, task_type AS Type "
                    + "FROM tasks WHERE id IN @ids",
                    new { ids = blocked.Keys.ToArray() },
                    cancellationToken: cancellationToken));

            foreach (var row in rows)
            {
                if (!TaskStates.IsTerminal(row.Status))
                {
                    blockedTaskStatuses[row.Id] = row.Status;
                }
            }
        }

        var labelCount = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(DISTINCT l.label) FROM labels l "
            + "INNER JOIN tasks t ON t.id = l.task_id WHERE t.status != @tombstone",
            new { tombstone = TaskStates.Tombstone, cancellationToken });

        var commentCount = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM comments");

        var eventCount = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM events");

        return new TaskStats
        {
            StatusCounts = statusCounts
                .OrderBy(StatusOrderIndex)
                .ThenBy(row => row.Value, StringComparer.Ordinal)
                .ToList(),
            ReadyCount = readyCount,
            BlockedTaskStatuses = blockedTaskStatuses,
            LabelCount = labelCount,
            CommentCount = commentCount,
            EventCount = eventCount
        };
    }

    public async Task<string?> GetConfigAsync(
        string key,
        CancellationToken cancellationToken)
    {
        await using var connection = await ConnectAsync(cancellationToken);

        return await GetConfigAsync(connection, key, cancellationToken);
    }

    public Task SetConfigAsync(
        string key,
        string value,
        CancellationToken cancellationToken)
        => throw NotImplemented();

    public async Task<IReadOnlyList<TaskConfigEntry>> ListConfigAsync(
        CancellationToken cancellationToken)
    {
        await using var connection = await ConnectAsync(cancellationToken);

        return (await connection.QueryAsync<TaskConfigEntry>(
            "SELECT key AS Key, value AS Value FROM config ORDER BY key ASC")).ToList();
    }

    public async Task<string> GetPrefixAsync(
        CancellationToken cancellationToken)
    {
        await using var connection = await ConnectAsync(cancellationToken);

        return await GetPrefixAsync(connection, cancellationToken);
    }

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

    private static (string WhereClause, DynamicParameters Parameters) BuildTaskFilterClause(
        TaskFilter filter)
    {
        var conditions = new List<string>();
        var parameters = new DynamicParameters();

        if (filter.Statuses is { Length: > 0 })
        {
            parameters.Add("statuses", filter.Statuses.Select(TaskStates.Normalize).ToArray());
            conditions.Add("status IN @statuses");
        }
        else if (!filter.IncludeAll)
        {
            parameters.Add("closedStatus", TaskStates.Closed);
            parameters.Add("tombstoneStatus", TaskStates.Tombstone);
            conditions.Add("status NOT IN (@closedStatus, @tombstoneStatus)");
        }

        if (filter.ExcludeTombstones)
        {
            parameters.Add("tombstone", TaskStates.Tombstone);
            conditions.Add("status != @tombstone");
        }

        if (!string.IsNullOrEmpty(filter.Type))
        {
            parameters.Add("type", TaskTypes.Normalize(filter.Type));
            conditions.Add("task_type = @type");
        }

        if (filter.Priority is { } priority)
        {
            parameters.Add("priority", priority);
            conditions.Add("priority = @priority");
        }

        if (filter.Unassigned)
        {
            conditions.Add("(assignee IS NULL OR assignee = '')");
        }
        else if (!string.IsNullOrEmpty(filter.Assignee))
        {
            parameters.Add("assignee", filter.Assignee);
            conditions.Add("assignee = @assignee");
        }

        if (filter.Labels is { Length: > 0 })
        {
            for (var i = 0; i < filter.Labels.Length; i++)
            {
                var parameterName = $"label{i}";
                parameters.Add(parameterName, filter.Labels[i].Trim().ToLowerInvariant());
                conditions.Add(
                    "EXISTS (SELECT 1 FROM labels WHERE task_id = tasks.id "
                    + $"AND label = @{parameterName})");
            }
        }

        if (!string.IsNullOrEmpty(filter.Text))
        {
            parameters.Add("text", EscapeLikeText(filter.Text));
            conditions.Add(
                "(LOWER(title) LIKE '%' || LOWER(@text) || '%' ESCAPE '\\' OR "
                + "LOWER(description) LIKE '%' || LOWER(@text) || '%' ESCAPE '\\' OR "
                + "LOWER(design) LIKE '%' || LOWER(@text) || '%' ESCAPE '\\' OR "
                + "LOWER(acceptance_criteria) LIKE '%' || LOWER(@text) || '%' ESCAPE '\\' OR "
                + "LOWER(notes) LIKE '%' || LOWER(@text) || '%' ESCAPE '\\')");
        }

        if (filter.UpdatedBefore is { } updatedBefore)
        {
            parameters.Add("updatedBefore", updatedBefore);
            conditions.Add("updated_at <= @updatedBefore");
        }

        if (filter.DeferredVisibleAt is { } visibleAt)
        {
            parameters.Add("visibleAt", visibleAt);
            conditions.Add("(defer_until IS NULL OR defer_until <= @visibleAt)");
        }

        var whereClause = conditions.Count > 0 ? $" WHERE {string.Join(" AND ", conditions)}" : "";

        return (whereClause, parameters);
    }

    private static string OrderByClause(TaskOrdering ordering) => ordering switch
    {
        TaskOrdering.PriorityCreatedId => " ORDER BY priority ASC, created_at ASC, id ASC",
        TaskOrdering.UpdatedAtAscending => " ORDER BY updated_at ASC, id ASC",
        TaskOrdering.ReadyPick =>
            " ORDER BY CASE WHEN priority <= 1 THEN 0 ELSE 1 END, created_at ASC, id ASC",
        _ => throw new ArgumentOutOfRangeException(nameof(ordering), ordering, "Unknown task ordering.")
    };

    /// <summary>
    /// Escapes the LIKE wildcard characters '%' and '_' (and the escape
    /// character itself) so search text is matched literally, other than the
    /// wildcards callers wrap around it.
    /// </summary>
    private static string EscapeLikeText(string value)
        => value.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");

    private static int StatusOrderIndex(TaskCount statusCount)
    {
        var index = Array.IndexOf(StatusOrder, statusCount.Value);

        return index < 0 ? StatusOrder.Length : index;
    }

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

    /// <summary>
    /// A group-by-priority row, kept numeric so it can be formatted as
    /// P0..P4.
    /// </summary>
    private sealed class PriorityCountRow
    {
        public required int Priority { get; init; }
        public required int Count { get; init; }
    }

    /// <summary>
    /// One group-by-value row: the grouped value and how many tasks fall
    /// into it.
    /// </summary>
    private sealed class CountRow
    {
        public required string Value { get; init; }
        public required int Count { get; init; }
    }

    /// <summary>
    /// A label's name and how many non-tombstone tasks carry it.
    /// </summary>
    private sealed class LabelCountRow
    {
        public required string Label { get; init; }
        public required int Count { get; init; }
    }
}
