using System.Data.Common;
using System.Globalization;
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

    private async Task<SqliteConnection> ConnectAsync(CancellationToken cancellationToken)
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

    private async Task<string?> GetConfigAsync(
        SqliteConnection connection,
        string key,
        CancellationToken cancellationToken,
        DbTransaction? transaction = null)
        => await connection.QueryFirstOrDefaultAsync<string>(
            "SELECT value FROM config WHERE key = @key",
            new { key, cancellationToken },
            transaction);

    private async Task SetConfigAsync(
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

    private async Task<string> GetPrefixAsync(
        SqliteConnection connection,
        CancellationToken cancellationToken,
        DbTransaction? transaction = null)
        => await GetConfigAsync(connection, PrefixConfigKey, cancellationToken, transaction)
            ?? TaskWorkspace.FallbackPrefix;

    private async Task<string> CreateTaskIdAsync(
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

    private async Task RecordEventAsync(
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

    private async Task<TaskItem?> GetTaskAsync(
        SqliteConnection connection,
        string id,
        CancellationToken cancellationToken,
        DbTransaction? transaction = null)
    {
        // Materializes an all-primitives row and parses the timestamps
        // itself, since the DateTimeOffset columns are stored as TEXT.
        var row = await connection.QueryFirstOrDefaultAsync<TaskRow>(
            $"SELECT {TaskItem.Columns} FROM tasks WHERE id = @id",
            new { id, cancellationToken },
            transaction);

        return row?.ToTaskItem();
    }

    private async Task<TaskItem> GetRequiredTaskAsync(
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

    private async Task<(
        IReadOnlyDictionary<string, IReadOnlyList<string>> Blocked,
        IReadOnlyDictionary<string, TaskGraphNode> Tasks)> ComputeBlockedAsync(
        SqliteConnection connection,
        CancellationToken cancellationToken)
    {
        // Both queries below run against the whole table and take no filter
        // parameters, so there is nothing for @-placeholder analysis to key
        // on; cancellation is checked up front instead of plumbed through a
        // parameter object.
        cancellationToken.ThrowIfCancellationRequested();

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

        var result = blocked.ToDictionary(
            pair => pair.Key,
            IReadOnlyList<string> (pair) =>
                [.. pair.Value.Order(StringComparer.Ordinal).Distinct()]);

        return (result, tasks);
    }

    // -------------------------------------------------------------------
    // New surface: backend-agnostic, no ADO.NET or SQLite types. Both the
    // read members (bd-oyf.2) and the write members (bd-oyf.3) below are
    // real implementations, each owning its own transaction and audit
    // events. No command calls this surface yet; that migration is done by
    // bd-oyf.4/bd-oyf.5.
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
            parameters["limit"] = sqlLimit;
            sql += " LIMIT @limit";
        }

        await using var connection = await ConnectAsync(cancellationToken);

        if (!filter.ExcludeBlocked)
        {
            return await ExecuteTaskQueryAsync(connection, sql, parameters, cancellationToken);
        }

        var (blocked, _) = await ComputeBlockedAsync(connection, cancellationToken);

        IEnumerable<TaskItem> tasks =
            await ExecuteTaskQueryAsync(connection, sql, parameters, cancellationToken);

        tasks = tasks.Where(t => !blocked.ContainsKey(t.Id));

        if (filter.Limit is { } limit)
        {
            tasks = tasks.Take(limit);
        }

        return tasks.ToList();
    }

    // The WHERE/ORDER/LIMIT clauses here are assembled at runtime from the
    // filter, so the SQL text is never a call-site literal; Dapper.AOT can
    // only intercept calls whose SQL it can read at compile time. Reading
    // through plain ADO.NET instead of Dapper's reflection fallback keeps
    // this path free of runtime code generation.
    private static async Task<List<TaskItem>> ExecuteTaskQueryAsync(
        SqliteConnection connection,
        string sql,
        IReadOnlyDictionary<string, object?> parameters,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        foreach (var (name, value) in parameters)
        {
            command.Parameters.AddWithValue("@" + name, value ?? DBNull.Value);
        }

        var results = new List<TaskItem>();

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var columns = TaskRowColumns.From(reader);

        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(columns.Read(reader).ToTaskItem());
        }

        return results;
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
        // column: SQLite's COUNT(*) always reads back as Int64, not Int32. A
        // settable property tolerates the narrowing.
        return (await connection.QueryAsync<LabelCountRow>(
                """
                SELECT l.label AS Label, COUNT(*) AS Count
                FROM labels l
                JOIN tasks t ON t.id = l.task_id
                WHERE t.status != @tombstoneStatus
                GROUP BY l.label
                ORDER BY l.label
                """,
                new { tombstoneStatus = TaskStates.Tombstone, cancellationToken }))
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

        // Materializes an all-primitives row and parses the timestamp
        // itself, since the created_at column is stored as TEXT. The query
        // takes no filter parameters, so cancellation here is best-effort
        // rather than plumbed through a parameter object.
        return (await connection.QueryAsync<TaskDependencyRow>(
                $"SELECT {TaskDependencyRow.Columns} FROM dependencies "
                + "ORDER BY task_id, depends_on_id"))
            .Select(r => r.ToTaskDependency())
            .ToList();
    }

    public async Task<IReadOnlyDictionary<string, IReadOnlyList<string>>> ComputeBlockedAsync(
        CancellationToken cancellationToken)
    {
        await using var connection = await ConnectAsync(cancellationToken);

        var (blocked, _) = await ComputeBlockedAsync(connection, cancellationToken);

        return blocked;
    }

    public async Task<IReadOnlyList<TaskEpicStatus>> GetEpicStatusesAsync(
        CancellationToken cancellationToken)
    {
        await using var connection = await ConnectAsync(cancellationToken);

        return await QueryEpicStatusesAsync(connection, cancellationToken);
    }

    private static async Task<IReadOnlyList<TaskEpicStatus>> QueryEpicStatusesAsync(
        SqliteConnection connection,
        CancellationToken cancellationToken)
        => (await connection.QueryAsync<TaskEpicStatus>(
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
                epic = TaskTypes.Epic,
                cancellationToken
            })).ToList();

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
        // column in every branch: SQLite's COUNT(*) always reads back as
        // Int64, not Int32. A settable property tolerates the narrowing.
        switch (dimension)
        {
            case TaskCountDimension.Status:
                return (await connection.QueryAsync<CountRow>(
                        "SELECT status AS Value, COUNT(*) AS Count FROM tasks "
                        + "WHERE status != @tombstone GROUP BY status ORDER BY status ASC",
                        new { tombstone = TaskStates.Tombstone, cancellationToken }))
                    .Select(r => new TaskCount(r.Value, r.Count))
                    .ToList();

            case TaskCountDimension.Type:
                return (await connection.QueryAsync<CountRow>(
                        "SELECT task_type AS Value, COUNT(*) AS Count FROM tasks "
                        + "WHERE status != @tombstone GROUP BY task_type ORDER BY task_type ASC",
                        new { tombstone = TaskStates.Tombstone, cancellationToken }))
                    .Select(r => new TaskCount(r.Value, r.Count))
                    .ToList();

            case TaskCountDimension.Priority:
                return (await connection.QueryAsync<PriorityCountRow>(
                        "SELECT priority AS Priority, COUNT(*) AS Count FROM tasks "
                        + "WHERE status != @tombstone GROUP BY priority ORDER BY priority ASC",
                        new { tombstone = TaskStates.Tombstone, cancellationToken }))
                    .Select(r => new TaskCount(TaskPriorities.Format(r.Priority), r.Count))
                    .ToList();

            case TaskCountDimension.Assignee:
                return (await connection.QueryAsync<CountRow>(
                        "SELECT COALESCE(NULLIF(assignee, ''), 'unassigned') AS Value, "
                        + "COUNT(*) AS Count FROM tasks WHERE status != @tombstone "
                        + "GROUP BY COALESCE(NULLIF(assignee, ''), 'unassigned') "
                        + "ORDER BY COALESCE(NULLIF(assignee, ''), 'unassigned') ASC",
                        new { tombstone = TaskStates.Tombstone, cancellationToken }))
                    .Select(r => new TaskCount(r.Value, r.Count))
                    .ToList();

            case TaskCountDimension.Label:
                return (await connection.QueryAsync<CountRow>(
                        "SELECT label AS Value, COUNT(*) AS Count FROM labels "
                        + "INNER JOIN tasks ON tasks.id = labels.task_id "
                        + "WHERE tasks.status != @tombstone GROUP BY label ORDER BY label ASC",
                        new { tombstone = TaskStates.Tombstone, cancellationToken }))
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
        // column: SQLite's COUNT(*) always reads back as Int64, not Int32. A
        // settable property tolerates the narrowing.
        var statusCounts = (await connection.QueryAsync<CountRow>(
                "SELECT status AS Value, COUNT(*) AS Count FROM tasks "
                + "WHERE status != @tombstone GROUP BY status",
                new { tombstone = TaskStates.Tombstone, cancellationToken }))
            .Select(r => new TaskCount(r.Value, r.Count))
            .ToList();

        var now = timeProvider.GetUtcNow();
        var (blocked, tasksById) = await ComputeBlockedAsync(connection, cancellationToken);

        var readyIds = await connection.QueryAsync<string>(
            "SELECT id FROM tasks WHERE status = @status "
            + "AND (defer_until IS NULL OR defer_until <= @now)",
            new { status = TaskStates.Open, now, cancellationToken });

        var readyCount = readyIds.Count(id => !blocked.ContainsKey(id));

        // Reuses the full task set ComputeBlockedAsync already loaded instead
        // of a second "id IN (...)" query, whose parameter count varies with
        // the blocked set and so is never a fixed, interceptable shape.
        var blockedTaskStatuses = new Dictionary<string, string>();

        foreach (var id in blocked.Keys)
        {
            if (tasksById.TryGetValue(id, out var node) && !TaskStates.IsTerminal(node.Status))
            {
                blockedTaskStatuses[id] = node.Status;
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

    public async Task SetConfigAsync(
        string key,
        string value,
        CancellationToken cancellationToken)
    {
        await using var connection = await ConnectAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        await SetConfigAsync(connection, key, value, cancellationToken, transaction);

        await transaction.CommitAsync(cancellationToken);
    }

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

    public async Task InitializeWorkspaceAsync(
        string workspaceDirectory,
        string prefix,
        CancellationToken cancellationToken)
    {
        await using var connection = await InitializeAsync(workspaceDirectory, cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        await SetConfigAsync(connection, PrefixConfigKey, prefix, cancellationToken, transaction);

        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<TaskCreationResult> CreateTaskAsync(
        TaskCreation creation,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var seed = $"{creation.Title}|{now:O}";

        await using var connection = await ConnectAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var resolvedDependencies = new List<(string TargetId, string Type, string Status)>();
        string id;

        if (creation.ParentId is not null)
        {
            var parent = await GetRequiredTaskAsync(
                connection, creation.ParentId, cancellationToken, transaction);
            resolvedDependencies.Add(
                (creation.ParentId, TaskDependencyTypes.ParentChild, parent.Status));
            id = await CreateTaskIdAsync(
                connection, creation.ParentId, seed, cancellationToken, transaction);
        }
        else
        {
            id = await CreateTaskIdAsync(connection, null, seed, cancellationToken, transaction);
        }

        foreach (var dependency in creation.DependsOn)
        {
            var target = await GetRequiredTaskAsync(
                connection, dependency.DependsOnId, cancellationToken, transaction);

            resolvedDependencies.Add((dependency.DependsOnId, dependency.Type, target.Status));
        }

        var task = new TaskItem
        {
            Id = id,
            Title = creation.Title,
            Description = creation.Description,
            Status = TaskStates.Open,
            Priority = creation.Priority,
            Type = creation.Type,
            Assignee = creation.Assignee,
            EstimatedMinutes = creation.EstimatedMinutes,
            DueAt = creation.DueAt,
            DeferUntil = creation.DeferUntil,
            CreatedAt = now,
            CreatedBy = creation.Actor,
            UpdatedAt = now
        };

        await connection.ExecuteAsync(
            "INSERT INTO tasks (id, title, description, design, acceptance_criteria, notes, "
            + "status, priority, task_type, assignee, estimated_minutes, due_at, defer_until, "
            + "created_at, created_by, updated_at, closed_at, close_reason, deleted_at, "
            + "delete_reason) "
            + "VALUES (@Id, @Title, @Description, @Design, @AcceptanceCriteria, @Notes, "
            + "@Status, @Priority, @Type, @Assignee, @EstimatedMinutes, @DueAt, @DeferUntil, "
            + "@CreatedAt, @CreatedBy, @UpdatedAt, @ClosedAt, @CloseReason, @DeletedAt, "
            + "@DeleteReason)",
            task,
            transaction);

        foreach (var label in creation.Labels)
        {
            await connection.ExecuteAsync(
                "INSERT OR IGNORE INTO labels (task_id, label) VALUES (@TaskId, @Label)",
                new { TaskId = id, Label = label, cancellationToken },
                transaction);
        }

        foreach (var (targetId, dependencyType, _) in resolvedDependencies)
        {
            await connection.ExecuteAsync(
                "INSERT OR IGNORE INTO dependencies "
                + "(task_id, depends_on_id, dependency_type, created_at, created_by) "
                + "VALUES (@TaskId, @DependsOnId, @Type, @CreatedAt, @CreatedBy)",
                new TaskDependency
                {
                    TaskId = id,
                    DependsOnId = targetId,
                    Type = dependencyType,
                    CreatedAt = now,
                    CreatedBy = creation.Actor
                },
                transaction);
        }

        await RecordEventAsync(
            connection,
            new TaskEvent
            {
                TaskId = id,
                Type = TaskEventTypes.Created,
                Actor = creation.Actor,
                CreatedAt = now
            },
            cancellationToken,
            transaction);

        await transaction.CommitAsync(cancellationToken);

        // A parent-child edge alone does not block the new task; only the
        // other blocking dependency types gate it (matching ComputeBlockedAsync).
        var blockedBy = resolvedDependencies
            .Where(d => d.Type != TaskDependencyTypes.ParentChild
                && TaskDependencyTypes.IsBlocking(d.Type)
                && !TaskStates.IsTerminal(d.Status))
            .Select(d => d.TargetId)
            .Distinct()
            .ToList();

        return new TaskCreationResult { Id = id, BlockedBy = blockedBy };
    }

    /// <summary>
    /// The in-memory outcome of validating and applying a
    /// <see cref="TaskUpdate"/> to a single task, before it is written.
    /// </summary>
    private sealed record TaskUpdateChange(
        TaskItem Task,
        List<string> ChangedFields,
        string? OldStatus,
        string? NewStatus,
        string? OldPriority,
        string? NewPriority,
        string? OldAssignee,
        string? NewAssignee);

    public async Task<TaskUpdateResult> UpdateTaskAsync(
        string id,
        TaskUpdate update,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();

        await using var connection = await ConnectAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        var task = await GetRequiredTaskAsync(connection, id, cancellationToken, transaction);

        var change = ApplyUpdate(task, update);
        task.UpdatedAt = now;

        await WriteUpdateAsync(connection, transaction, task, change, update.Actor, now, cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return new TaskUpdateResult { ChangedFields = change.ChangedFields };
    }

    /// <summary>
    /// Applies the given field changes to every task and records the
    /// corresponding events for each. Every task is loaded and validated
    /// before any is written: either every task updates or none does,
    /// mirroring <see cref="CloseTaskAsync"/>. Throws
    /// <see cref="ExitException"/> when any task does not exist, is a
    /// tombstone, or a status guard is violated.
    /// </summary>
    public async Task<IReadOnlyList<TaskItem>> UpdateTasksAsync(
        IReadOnlyList<string> ids,
        TaskUpdate update,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();

        await using var connection = await ConnectAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        // Every task is loaded and validated (and has the update applied
        // in-memory) before any write happens, which gives the bulk update
        // its all-or-nothing behavior: nothing is written until every id has
        // passed.
        var changes = new List<TaskUpdateChange>();

        foreach (var id in ids)
        {
            var task = await GetRequiredTaskAsync(connection, id, cancellationToken, transaction);
            changes.Add(ApplyUpdate(task, update));
        }

        foreach (var change in changes)
        {
            change.Task.UpdatedAt = now;

            await WriteUpdateAsync(
                connection, transaction, change.Task, change, update.Actor, now, cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);

        return changes.Select(change => change.Task).ToArray();
    }

    /// <summary>
    /// Validates the given update against the task's current state and
    /// applies the resulting field changes to <paramref name="task"/> in
    /// memory. Performs no I/O. Throws <see cref="ExitException"/> when a
    /// status guard is violated.
    /// </summary>
    private static TaskUpdateChange ApplyUpdate(TaskItem task, TaskUpdate update)
    {
        var changedFields = new List<string>();
        string? oldStatus = null;
        string? newStatus = null;
        string? oldPriority = null;
        string? newPriority = null;
        string? oldAssignee = null;
        string? newAssignee = null;

        if (update.TitleGiven)
        {
            var title = update.Title ?? "";

            if (title.Length is 0 or > 500)
            {
                throw new ExitException("The title must be 1-500 characters.");
            }

            if (title != task.Title)
            {
                task.Title = title;
                changedFields.Add("title");
            }
        }

        if (update.DescriptionGiven)
        {
            var description = update.Description ?? "";

            if (description != task.Description)
            {
                task.Description = description;
                changedFields.Add("description");
            }
        }

        if (update.TypeGiven)
        {
            var type = TaskTypes.Normalize(update.Type ?? "");

            if (type != task.Type)
            {
                task.Type = type;
                changedFields.Add("task_type");
            }
        }

        if (update.NotesGiven)
        {
            var notes = update.Notes ?? "";

            if (notes != task.Notes)
            {
                task.Notes = notes;
                changedFields.Add("notes");
            }
        }

        if (update.DesignGiven)
        {
            var design = update.Design ?? "";

            if (design != task.Design)
            {
                task.Design = design;
                changedFields.Add("design");
            }
        }

        if (update.AcceptanceCriteriaGiven)
        {
            var acceptanceCriteria = update.AcceptanceCriteria ?? "";

            if (acceptanceCriteria != task.AcceptanceCriteria)
            {
                task.AcceptanceCriteria = acceptanceCriteria;
                changedFields.Add("acceptance_criteria");
            }
        }

        if (update.DueAtGiven)
        {
            if (update.DueAt != task.DueAt)
            {
                task.DueAt = update.DueAt;
                changedFields.Add("due_at");
            }
        }

        if (update.DeferUntilGiven)
        {
            if (update.DeferUntil != task.DeferUntil)
            {
                task.DeferUntil = update.DeferUntil;
                changedFields.Add("defer_until");
            }
        }

        if (update.EstimatedMinutesGiven)
        {
            if (update.EstimatedMinutes != task.EstimatedMinutes)
            {
                task.EstimatedMinutes = update.EstimatedMinutes;
                changedFields.Add("estimated_minutes");
            }
        }

        if (update.StatusGiven)
        {
            var status = TaskStates.Normalize(update.Status ?? "");

            if (status == TaskStates.Closed)
            {
                throw new ExitException("Use `nitro task close` to close a task.");
            }

            if (status == TaskStates.Tombstone)
            {
                throw new ExitException("Use `nitro task delete` to delete a task.");
            }

            if (task.Status == TaskStates.Closed)
            {
                throw new ExitException("Use `nitro task reopen` to reopen a task.");
            }

            if (status != task.Status)
            {
                oldStatus = task.Status;
                newStatus = status;
                task.Status = status;
            }
        }

        if (update.PriorityGiven)
        {
            var priority = update.Priority ?? TaskPriorities.Medium;

            if (priority != task.Priority)
            {
                oldPriority = TaskPriorities.Format(task.Priority);
                newPriority = TaskPriorities.Format(priority);
                task.Priority = priority;
            }
        }

        if (update.AssigneeGiven)
        {
            var assignee = string.IsNullOrEmpty(update.Assignee) ? null : update.Assignee;

            if (assignee != task.Assignee)
            {
                oldAssignee = task.Assignee ?? "";
                newAssignee = assignee ?? "";
                task.Assignee = assignee;
            }
        }

        return new TaskUpdateChange(
            task, changedFields, oldStatus, newStatus, oldPriority, newPriority, oldAssignee, newAssignee);
    }

    /// <summary>
    /// Persists a previously-computed <see cref="TaskUpdateChange"/> and
    /// records the corresponding events. Performs no validation, since
    /// <see cref="ApplyUpdate"/> already validated and applied the change to
    /// <paramref name="task"/> in memory.
    /// </summary>
    private async Task WriteUpdateAsync(
        SqliteConnection connection,
        DbTransaction transaction,
        TaskItem task,
        TaskUpdateChange change,
        string actor,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        await connection.ExecuteAsync(
            """
            UPDATE tasks
            SET title = @Title,
                description = @Description,
                design = @Design,
                acceptance_criteria = @AcceptanceCriteria,
                notes = @Notes,
                status = @Status,
                priority = @Priority,
                task_type = @Type,
                assignee = @Assignee,
                estimated_minutes = @EstimatedMinutes,
                due_at = @DueAt,
                defer_until = @DeferUntil,
                updated_at = @UpdatedAt
            WHERE id = @Id
            """,
            new
            {
                task.Title,
                task.Description,
                task.Design,
                task.AcceptanceCriteria,
                task.Notes,
                task.Status,
                task.Priority,
                task.Type,
                task.Assignee,
                task.EstimatedMinutes,
                task.DueAt,
                task.DeferUntil,
                task.UpdatedAt,
                Id = task.Id,
                cancellationToken
            },
            transaction);

        if (change.OldStatus is not null)
        {
            await RecordEventAsync(
                connection,
                new TaskEvent
                {
                    TaskId = task.Id,
                    Type = TaskEventTypes.StatusChanged,
                    Actor = actor,
                    OldValue = change.OldStatus,
                    NewValue = change.NewStatus,
                    CreatedAt = now
                },
                cancellationToken,
                transaction);
        }

        if (change.OldPriority is not null)
        {
            await RecordEventAsync(
                connection,
                new TaskEvent
                {
                    TaskId = task.Id,
                    Type = TaskEventTypes.PriorityChanged,
                    Actor = actor,
                    OldValue = change.OldPriority,
                    NewValue = change.NewPriority,
                    CreatedAt = now
                },
                cancellationToken,
                transaction);
        }

        if (change.OldAssignee is not null)
        {
            await RecordEventAsync(
                connection,
                new TaskEvent
                {
                    TaskId = task.Id,
                    Type = TaskEventTypes.AssigneeChanged,
                    Actor = actor,
                    OldValue = change.OldAssignee,
                    NewValue = change.NewAssignee,
                    CreatedAt = now
                },
                cancellationToken,
                transaction);
        }

        if (change.ChangedFields.Count > 0)
        {
            await RecordEventAsync(
                connection,
                new TaskEvent
                {
                    TaskId = task.Id,
                    Type = TaskEventTypes.Updated,
                    Actor = actor,
                    Comment = string.Join(", ", change.ChangedFields),
                    CreatedAt = now
                },
                cancellationToken,
                transaction);
        }
    }

    public async Task<IReadOnlyList<TaskItem>> CloseTaskAsync(
        IReadOnlyList<string> ids,
        string reason,
        string actor,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();

        await using var connection = await ConnectAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        // Every task is loaded and validated before any write happens, which
        // gives close its all-or-nothing behavior: nothing is written until
        // every id has passed.
        var tasks = new List<TaskItem>();

        foreach (var id in ids)
        {
            var task = await GetRequiredTaskAsync(connection, id, cancellationToken, transaction);

            if (task.Status == TaskStates.Closed)
            {
                throw new ExitException($"Task '{id}' is already closed.");
            }

            tasks.Add(task);
        }

        foreach (var task in tasks)
        {
            var oldStatus = task.Status;

            await connection.ExecuteAsync(
                "UPDATE tasks SET status = @status, closed_at = @closedAt, "
                + "close_reason = @closeReason, updated_at = @updatedAt WHERE id = @id",
                new
                {
                    status = TaskStates.Closed,
                    closedAt = now,
                    closeReason = reason,
                    updatedAt = now,
                    id = task.Id,
                    cancellationToken
                },
                transaction);

            await RecordEventAsync(
                connection,
                new TaskEvent
                {
                    TaskId = task.Id,
                    Type = TaskEventTypes.Closed,
                    Actor = actor,
                    OldValue = oldStatus,
                    NewValue = TaskStates.Closed,
                    Comment = string.IsNullOrEmpty(reason) ? null : reason,
                    CreatedAt = now
                },
                cancellationToken,
                transaction);

            task.Status = TaskStates.Closed;
            task.ClosedAt = now;
            task.CloseReason = reason;
            task.UpdatedAt = now;
        }

        await transaction.CommitAsync(cancellationToken);

        return tasks;
    }

    public async Task<TaskItem> ReopenTaskAsync(
        string id,
        string reason,
        string actor,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();

        await using var connection = await ConnectAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        var task = await GetRequiredTaskAsync(connection, id, cancellationToken, transaction);

        if (task.Status != TaskStates.Closed)
        {
            throw new ExitException($"Task '{id}' is not closed.");
        }

        var oldStatus = task.Status;

        await connection.ExecuteAsync(
            "UPDATE tasks SET status = @status, closed_at = NULL, "
            + "close_reason = @closeReason, updated_at = @updatedAt WHERE id = @id",
            new
            {
                status = TaskStates.Open,
                closeReason = "",
                updatedAt = now,
                id = task.Id,
                cancellationToken
            },
            transaction);

        await RecordEventAsync(
            connection,
            new TaskEvent
            {
                TaskId = task.Id,
                Type = TaskEventTypes.Reopened,
                Actor = actor,
                OldValue = oldStatus,
                NewValue = TaskStates.Open,
                Comment = string.IsNullOrEmpty(reason) ? null : reason,
                CreatedAt = now
            },
            cancellationToken,
            transaction);

        await transaction.CommitAsync(cancellationToken);

        task.Status = TaskStates.Open;
        task.ClosedAt = null;
        task.CloseReason = "";
        task.UpdatedAt = now;

        return task;
    }

    public async Task<TaskItem> DeferTaskAsync(
        string id,
        DateTimeOffset until,
        string actor,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();

        await using var connection = await ConnectAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        var task = await GetRequiredTaskAsync(connection, id, cancellationToken, transaction);

        if (task.Status is not (TaskStates.Open or TaskStates.InProgress))
        {
            throw new ExitException("Only open or in-progress tasks can be deferred.");
        }

        var oldStatus = task.Status;

        await connection.ExecuteAsync(
            "UPDATE tasks SET status = @status, defer_until = @deferUntil, "
            + "updated_at = @updatedAt WHERE id = @id",
            new
            {
                status = TaskStates.Deferred,
                deferUntil = until,
                updatedAt = now,
                id = task.Id,
                cancellationToken
            },
            transaction);

        await RecordEventAsync(
            connection,
            new TaskEvent
            {
                TaskId = task.Id,
                Type = TaskEventTypes.Deferred,
                Actor = actor,
                OldValue = oldStatus,
                NewValue = TaskDates.Format(until),
                CreatedAt = now
            },
            cancellationToken,
            transaction);

        await transaction.CommitAsync(cancellationToken);

        task.Status = TaskStates.Deferred;
        task.DeferUntil = until;
        task.UpdatedAt = now;

        return task;
    }

    public async Task<TaskItem> UndeferTaskAsync(
        string id,
        string actor,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();

        await using var connection = await ConnectAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        var task = await GetRequiredTaskAsync(connection, id, cancellationToken, transaction);

        if (task.Status != TaskStates.Deferred)
        {
            throw new ExitException($"Task '{id}' is not deferred.");
        }

        var oldStatus = task.Status;

        await connection.ExecuteAsync(
            "UPDATE tasks SET status = @status, defer_until = NULL, "
            + "updated_at = @updatedAt WHERE id = @id",
            new
            {
                status = TaskStates.Open,
                updatedAt = now,
                id = task.Id,
                cancellationToken
            },
            transaction);

        await RecordEventAsync(
            connection,
            new TaskEvent
            {
                TaskId = task.Id,
                Type = TaskEventTypes.Undeferred,
                Actor = actor,
                OldValue = oldStatus,
                NewValue = TaskStates.Open,
                CreatedAt = now
            },
            cancellationToken,
            transaction);

        await transaction.CommitAsync(cancellationToken);

        task.Status = TaskStates.Open;
        task.DeferUntil = null;
        task.UpdatedAt = now;

        return task;
    }

    public async Task<TaskItem> DeleteTaskAsync(
        string id,
        string reason,
        string actor,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();

        await using var connection = await ConnectAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        var task = await GetRequiredTaskAsync(connection, id, cancellationToken, transaction);

        var oldStatus = task.Status;

        await connection.ExecuteAsync(
            "UPDATE tasks SET status = @status, deleted_at = @deletedAt, "
            + "delete_reason = @deleteReason, updated_at = @updatedAt WHERE id = @id",
            new
            {
                status = TaskStates.Tombstone,
                deletedAt = now,
                deleteReason = reason,
                updatedAt = now,
                id = task.Id,
                cancellationToken
            },
            transaction);

        await RecordEventAsync(
            connection,
            new TaskEvent
            {
                TaskId = task.Id,
                Type = TaskEventTypes.Deleted,
                Actor = actor,
                OldValue = oldStatus,
                NewValue = TaskStates.Tombstone,
                Comment = string.IsNullOrEmpty(reason) ? null : reason,
                CreatedAt = now
            },
            cancellationToken,
            transaction);

        await transaction.CommitAsync(cancellationToken);

        task.Status = TaskStates.Tombstone;
        task.DeletedAt = now;
        task.DeleteReason = reason;
        task.UpdatedAt = now;

        return task;
    }

    public async Task<IReadOnlyList<TaskEpicStatus>> CloseEligibleEpicsAsync(
        string actor,
        CancellationToken cancellationToken)
    {
        const string closeReason = "All children are closed.";
        var now = timeProvider.GetUtcNow();

        await using var connection = await ConnectAsync(cancellationToken);

        var epics = await QueryEpicStatusesAsync(connection, cancellationToken);
        var eligible = epics.Where(epic => epic.IsEligibleForClose).ToList();

        if (eligible.Count == 0)
        {
            return eligible;
        }

        // Every epic is validated up front (IsEligibleForClose, checked
        // against data read before the transaction opens); the update loop
        // below is then all-or-nothing, matching CloseTaskCommand's pattern.
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        foreach (var epic in eligible)
        {
            await connection.ExecuteAsync(
                "UPDATE tasks SET status = @status, closed_at = @closedAt, "
                + "close_reason = @closeReason, updated_at = @updatedAt WHERE id = @id",
                new
                {
                    status = TaskStates.Closed,
                    closedAt = now,
                    closeReason,
                    updatedAt = now,
                    id = epic.Id,
                    cancellationToken
                },
                transaction);

            await RecordEventAsync(
                connection,
                new TaskEvent
                {
                    TaskId = epic.Id,
                    Type = TaskEventTypes.Closed,
                    Actor = actor,
                    OldValue = epic.Status,
                    NewValue = TaskStates.Closed,
                    Comment = closeReason,
                    CreatedAt = now
                },
                cancellationToken,
                transaction);
        }

        await transaction.CommitAsync(cancellationToken);

        return eligible.Select(epic => epic with { Status = TaskStates.Closed }).ToList();
    }

    public async Task<TaskComment> AddCommentAsync(
        string id,
        string text,
        string actor,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ExitException("The comment text must not be empty.");
        }

        var now = timeProvider.GetUtcNow();

        await using var connection = await ConnectAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        var task = await GetRequiredTaskAsync(connection, id, cancellationToken, transaction);

        var commentId = await connection.ExecuteScalarAsync<long>(
            "INSERT INTO comments (task_id, author, text, created_at) "
            + "VALUES (@TaskId, @Author, @Text, @CreatedAt) RETURNING id",
            new { TaskId = task.Id, Author = actor, Text = text, CreatedAt = now, cancellationToken },
            transaction);

        await connection.ExecuteAsync(
            "UPDATE tasks SET updated_at = @updatedAt WHERE id = @id",
            new { updatedAt = now, id = task.Id, cancellationToken },
            transaction);

        await RecordEventAsync(
            connection,
            new TaskEvent
            {
                TaskId = task.Id,
                Type = TaskEventTypes.Commented,
                Actor = actor,
                CreatedAt = now
            },
            cancellationToken,
            transaction);

        await transaction.CommitAsync(cancellationToken);

        return new TaskComment
        {
            Id = commentId,
            TaskId = task.Id,
            Author = actor,
            Text = text,
            CreatedAt = now
        };
    }

    public async Task<IReadOnlyList<TaskLabelChange>> AddLabelAsync(
        string id,
        IReadOnlyList<string> labels,
        string actor,
        CancellationToken cancellationToken)
    {
        var normalizedLabels = labels.Select(label => label.Trim().ToLowerInvariant()).ToList();

        if (normalizedLabels.Any(label => label.Length == 0))
        {
            throw new ExitException("Labels must be non-empty.");
        }

        var now = timeProvider.GetUtcNow();

        await using var connection = await ConnectAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        var task = await GetRequiredTaskAsync(connection, id, cancellationToken, transaction);

        var results = new List<TaskLabelChange>();

        foreach (var label in normalizedLabels)
        {
            var rowsAffected = await connection.ExecuteAsync(
                "INSERT OR IGNORE INTO labels (task_id, label) VALUES (@TaskId, @Label)",
                new { TaskId = task.Id, Label = label, cancellationToken },
                transaction);

            results.Add(new TaskLabelChange(label, rowsAffected > 0));
        }

        if (results.Any(result => result.Added))
        {
            await connection.ExecuteAsync(
                "UPDATE tasks SET updated_at = @updatedAt WHERE id = @id",
                new { updatedAt = now, id = task.Id, cancellationToken },
                transaction);
        }

        foreach (var result in results)
        {
            if (result.Added)
            {
                await RecordEventAsync(
                    connection,
                    new TaskEvent
                    {
                        TaskId = task.Id,
                        Type = TaskEventTypes.LabelAdded,
                        Actor = actor,
                        NewValue = result.Label,
                        CreatedAt = now
                    },
                    cancellationToken,
                    transaction);
            }
        }

        await transaction.CommitAsync(cancellationToken);

        return results;
    }

    public async Task RemoveLabelAsync(
        string id,
        string label,
        string actor,
        CancellationToken cancellationToken)
    {
        var normalizedLabel = label.Trim().ToLowerInvariant();
        var now = timeProvider.GetUtcNow();

        await using var connection = await ConnectAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        var task = await GetRequiredTaskAsync(connection, id, cancellationToken, transaction);

        var exists = await connection.ExecuteScalarAsync<long>(
            "SELECT COUNT(*) FROM labels WHERE task_id = @TaskId AND label = @Label",
            new { TaskId = task.Id, Label = normalizedLabel, cancellationToken },
            transaction);

        if (exists == 0)
        {
            throw new ExitException($"Label '{normalizedLabel}' is not on '{task.Id}'.");
        }

        await connection.ExecuteAsync(
            "DELETE FROM labels WHERE task_id = @TaskId AND label = @Label",
            new { TaskId = task.Id, Label = normalizedLabel, cancellationToken },
            transaction);

        await connection.ExecuteAsync(
            "UPDATE tasks SET updated_at = @updatedAt WHERE id = @id",
            new { updatedAt = now, id = task.Id, cancellationToken },
            transaction);

        await RecordEventAsync(
            connection,
            new TaskEvent
            {
                TaskId = task.Id,
                Type = TaskEventTypes.LabelRemoved,
                Actor = actor,
                OldValue = normalizedLabel,
                CreatedAt = now
            },
            cancellationToken,
            transaction);

        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<TaskDependencyAddResult> AddDependencyAsync(
        string id,
        string dependsOnId,
        string type,
        string actor,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();

        await using var connection = await ConnectAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        await GetRequiredTaskAsync(connection, id, cancellationToken, transaction);
        await GetRequiredTaskAsync(connection, dependsOnId, cancellationToken, transaction);

        if (id == dependsOnId)
        {
            throw new ExitException("A task cannot depend on itself.");
        }

        var existingCount = await connection.ExecuteScalarAsync<long>(
            "SELECT COUNT(*) FROM dependencies WHERE task_id = @id AND depends_on_id = @dependsOnId",
            new { id, dependsOnId, cancellationToken },
            transaction);

        if (existingCount > 0)
        {
            throw new ExitException("Dependency already exists.");
        }

        await connection.ExecuteAsync(
            "INSERT INTO dependencies "
            + "(task_id, depends_on_id, dependency_type, created_at, created_by) "
            + "VALUES (@TaskId, @DependsOnId, @Type, @CreatedAt, @CreatedBy)",
            new TaskDependency
            {
                TaskId = id,
                DependsOnId = dependsOnId,
                Type = type,
                CreatedAt = now,
                CreatedBy = actor
            },
            transaction);

        await connection.ExecuteAsync(
            "UPDATE tasks SET updated_at = @updatedAt WHERE id = @id",
            new { updatedAt = now, id, cancellationToken },
            transaction);

        await RecordEventAsync(
            connection,
            new TaskEvent
            {
                TaskId = id,
                Type = TaskEventTypes.DependencyAdded,
                Actor = actor,
                OldValue = null,
                NewValue = $"{type}:{dependsOnId}",
                CreatedAt = now
            },
            cancellationToken,
            transaction);

        List<string>? cycle = null;

        if (TaskDependencyTypes.IsBlocking(type))
        {
            cycle = await FindBlockingCycleAsync(connection, transaction, id, dependsOnId);
        }

        await transaction.CommitAsync(cancellationToken);

        return new TaskDependencyAddResult { Cycle = cycle };
    }

    public async Task RemoveDependencyAsync(
        string id,
        string dependsOnId,
        string actor,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();

        await using var connection = await ConnectAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        await GetRequiredTaskAsync(connection, id, cancellationToken, transaction);

        var existingType = await connection.QueryFirstOrDefaultAsync<string>(
            "SELECT dependency_type FROM dependencies "
            + "WHERE task_id = @id AND depends_on_id = @dependsOnId",
            new { id, dependsOnId, cancellationToken },
            transaction);

        if (existingType is null)
        {
            throw new ExitException("Dependency does not exist.");
        }

        await connection.ExecuteAsync(
            "DELETE FROM dependencies WHERE task_id = @id AND depends_on_id = @dependsOnId",
            new { id, dependsOnId, cancellationToken },
            transaction);

        await connection.ExecuteAsync(
            "UPDATE tasks SET updated_at = @updatedAt WHERE id = @id",
            new { updatedAt = now, id, cancellationToken },
            transaction);

        await RecordEventAsync(
            connection,
            new TaskEvent
            {
                TaskId = id,
                Type = TaskEventTypes.DependencyRemoved,
                Actor = actor,
                OldValue = null,
                NewValue = $"{existingType}:{dependsOnId}",
                CreatedAt = now
            },
            cancellationToken,
            transaction);

        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<TaskDependencyAddResult> SetParentAsync(
        string id,
        string? parentId,
        string actor,
        CancellationToken cancellationToken)
    {
        var normalizedParentId = string.IsNullOrEmpty(parentId) ? null : parentId;

        if (normalizedParentId == id)
        {
            throw new ExitException("A task cannot be its own parent.");
        }

        var now = timeProvider.GetUtcNow();

        await using var connection = await ConnectAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        await GetRequiredTaskAsync(connection, id, cancellationToken, transaction);

        if (normalizedParentId is not null)
        {
            await GetRequiredTaskAsync(connection, normalizedParentId, cancellationToken, transaction);
        }

        var existingParentIds = (await connection.QueryAsync<string>(
                "SELECT depends_on_id FROM dependencies "
                + "WHERE task_id = @id AND dependency_type = @type",
                new { id, type = TaskDependencyTypes.ParentChild, cancellationToken },
                transaction))
            .ToList();

        if (existingParentIds.Count == 1 && existingParentIds[0] == normalizedParentId)
        {
            await transaction.CommitAsync(cancellationToken);
            return new TaskDependencyAddResult { Cycle = null };
        }

        if (existingParentIds.Count == 0 && normalizedParentId is null)
        {
            await transaction.CommitAsync(cancellationToken);
            return new TaskDependencyAddResult { Cycle = null };
        }

        foreach (var existingParentId in existingParentIds)
        {
            await connection.ExecuteAsync(
                "DELETE FROM dependencies "
                + "WHERE task_id = @id AND depends_on_id = @existingParentId AND dependency_type = @type",
                new { id, existingParentId, type = TaskDependencyTypes.ParentChild, cancellationToken },
                transaction);

            await RecordEventAsync(
                connection,
                new TaskEvent
                {
                    TaskId = id,
                    Type = TaskEventTypes.DependencyRemoved,
                    Actor = actor,
                    OldValue = null,
                    NewValue = $"{TaskDependencyTypes.ParentChild}:{existingParentId}",
                    CreatedAt = now
                },
                cancellationToken,
                transaction);
        }

        List<string>? cycle = null;

        if (normalizedParentId is not null)
        {
            var existingCount = await connection.ExecuteScalarAsync<long>(
                "SELECT COUNT(*) FROM dependencies WHERE task_id = @id AND depends_on_id = @normalizedParentId",
                new { id, normalizedParentId, cancellationToken },
                transaction);

            if (existingCount > 0)
            {
                throw new ExitException("Dependency already exists.");
            }

            await connection.ExecuteAsync(
                "INSERT INTO dependencies "
                + "(task_id, depends_on_id, dependency_type, created_at, created_by) "
                + "VALUES (@TaskId, @DependsOnId, @Type, @CreatedAt, @CreatedBy)",
                new TaskDependency
                {
                    TaskId = id,
                    DependsOnId = normalizedParentId,
                    Type = TaskDependencyTypes.ParentChild,
                    CreatedAt = now,
                    CreatedBy = actor
                },
                transaction);

            await RecordEventAsync(
                connection,
                new TaskEvent
                {
                    TaskId = id,
                    Type = TaskEventTypes.DependencyAdded,
                    Actor = actor,
                    OldValue = null,
                    NewValue = $"{TaskDependencyTypes.ParentChild}:{normalizedParentId}",
                    CreatedAt = now
                },
                cancellationToken,
                transaction);

            cycle = await FindBlockingCycleAsync(connection, transaction, id, normalizedParentId);
        }

        await connection.ExecuteAsync(
            "UPDATE tasks SET updated_at = @updatedAt WHERE id = @id",
            new { updatedAt = now, id, cancellationToken },
            transaction);

        await transaction.CommitAsync(cancellationToken);

        return new TaskDependencyAddResult { Cycle = cycle };
    }

    public async Task EnsureWorkspaceAsync(
        string workspaceDirectory,
        CancellationToken cancellationToken)
    {
        if (fileSystem.FileExists(TaskWorkspace.GetDatabasePath(workspaceDirectory)))
        {
            return;
        }

        if (!fileSystem.DirectoryExists(workspaceDirectory))
        {
            fileSystem.CreateDirectory(workspaceDirectory);
        }

        await using var connection = await InitializeAsync(workspaceDirectory, cancellationToken);
    }

    public async Task<IReadOnlyList<TaskSyncRecord>> ExportTasksAsync(
        CancellationToken cancellationToken)
    {
        // Every query below runs against a whole table and takes no filter
        // parameters, so there is nothing for @-placeholder analysis to key
        // on; cancellation is checked up front instead of plumbed through a
        // parameter object (see ComputeBlockedAsync).
        cancellationToken.ThrowIfCancellationRequested();

        await using var connection = await ConnectAsync(cancellationToken);

        var taskRows = await connection.QueryAsync<TaskRow>(
            $"SELECT {TaskItem.Columns} FROM tasks ORDER BY id");

        var labelsByTask = (await connection.QueryAsync<TaskLabelRow>(
                "SELECT task_id AS TaskId, label AS Label FROM labels ORDER BY task_id, label"))
            .GroupBy(row => row.TaskId)
            .ToDictionary(
                group => group.Key,
                IReadOnlyList<string> (group) => [.. group.Select(row => row.Label)]);

        var dependenciesByTask = (await connection.QueryAsync<TaskDependencyRow>(
                $"SELECT {TaskDependencyRow.Columns} FROM dependencies "
                + "ORDER BY task_id, depends_on_id"))
            .GroupBy(row => row.TaskId)
            .ToDictionary(
                group => group.Key,
                IReadOnlyList<TaskSyncDependency> (group) =>
                    [.. group.Select(row => new TaskSyncDependency
                    {
                        DependsOnId = row.DependsOnId,
                        Type = row.Type,
                        CreatedAt = DateTimeOffset.Parse(row.CreatedAt, CultureInfo.InvariantCulture),
                        CreatedBy = row.CreatedBy
                    })]);

        var commentsByTask = (await connection.QueryAsync<TaskCommentRow>(
                $"SELECT {TaskComment.Columns} FROM comments ORDER BY task_id, created_at, id"))
            .GroupBy(row => row.TaskId)
            .ToDictionary(
                group => group.Key,
                IReadOnlyList<TaskSyncComment> (group) =>
                    [.. group.Select(row => new TaskSyncComment
                    {
                        Id = row.Id,
                        Author = row.Author,
                        Text = row.Text,
                        CreatedAt = DateTimeOffset.Parse(row.CreatedAt, CultureInfo.InvariantCulture)
                    })]);

        return taskRows.Select(row =>
        {
            var task = row.ToTaskItem();

            return new TaskSyncRecord
            {
                Id = task.Id,
                Title = task.Title,
                Description = task.Description,
                Design = task.Design,
                AcceptanceCriteria = task.AcceptanceCriteria,
                Notes = task.Notes,
                Status = task.Status,
                Priority = task.Priority,
                Type = task.Type,
                Assignee = task.Assignee,
                EstimatedMinutes = task.EstimatedMinutes,
                DueAt = task.DueAt,
                DeferUntil = task.DeferUntil,
                CreatedAt = task.CreatedAt,
                CreatedBy = task.CreatedBy,
                UpdatedAt = task.UpdatedAt,
                ClosedAt = task.ClosedAt,
                CloseReason = task.CloseReason,
                DeletedAt = task.DeletedAt,
                DeleteReason = task.DeleteReason,
                Labels = labelsByTask.GetValueOrDefault(task.Id, []),
                Dependencies = dependenciesByTask.GetValueOrDefault(task.Id, []),
                Comments = commentsByTask.GetValueOrDefault(task.Id, [])
            };
        }).ToList();
    }

    public async Task<TaskImportResult> ImportTasksAsync(
        IReadOnlyList<TaskSyncRecord> records,
        CancellationToken cancellationToken)
    {
        await using var connection = await ConnectAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var applied = 0;
        var skipped = 0;

        foreach (var record in records)
        {
            var existing = await GetTaskAsync(connection, record.Id, cancellationToken, transaction);

            if (existing is not null && existing.UpdatedAt > record.UpdatedAt)
            {
                skipped++;
                continue;
            }

            await connection.ExecuteAsync(
                """
                INSERT INTO tasks (id, title, description, design, acceptance_criteria, notes,
                    status, priority, task_type, assignee, estimated_minutes, due_at, defer_until,
                    created_at, created_by, updated_at, closed_at, close_reason, deleted_at,
                    delete_reason)
                VALUES (@Id, @Title, @Description, @Design, @AcceptanceCriteria, @Notes,
                    @Status, @Priority, @Type, @Assignee, @EstimatedMinutes, @DueAt, @DeferUntil,
                    @CreatedAt, @CreatedBy, @UpdatedAt, @ClosedAt, @CloseReason, @DeletedAt,
                    @DeleteReason)
                ON CONFLICT (id) DO UPDATE SET
                    title = excluded.title,
                    description = excluded.description,
                    design = excluded.design,
                    acceptance_criteria = excluded.acceptance_criteria,
                    notes = excluded.notes,
                    status = excluded.status,
                    priority = excluded.priority,
                    task_type = excluded.task_type,
                    assignee = excluded.assignee,
                    estimated_minutes = excluded.estimated_minutes,
                    due_at = excluded.due_at,
                    defer_until = excluded.defer_until,
                    created_at = excluded.created_at,
                    created_by = excluded.created_by,
                    updated_at = excluded.updated_at,
                    closed_at = excluded.closed_at,
                    close_reason = excluded.close_reason,
                    deleted_at = excluded.deleted_at,
                    delete_reason = excluded.delete_reason
                """,
                record,
                transaction);

            await connection.ExecuteAsync(
                "DELETE FROM labels WHERE task_id = @TaskId",
                new { TaskId = record.Id, cancellationToken },
                transaction);

            foreach (var label in record.Labels)
            {
                await connection.ExecuteAsync(
                    "INSERT INTO labels (task_id, label) VALUES (@TaskId, @Label)",
                    new { TaskId = record.Id, Label = label, cancellationToken },
                    transaction);
            }

            await connection.ExecuteAsync(
                "DELETE FROM dependencies WHERE task_id = @TaskId",
                new { TaskId = record.Id, cancellationToken },
                transaction);

            foreach (var dependency in record.Dependencies)
            {
                await connection.ExecuteAsync(
                    "INSERT INTO dependencies "
                    + "(task_id, depends_on_id, dependency_type, created_at, created_by) "
                    + "VALUES (@TaskId, @DependsOnId, @Type, @CreatedAt, @CreatedBy)",
                    new
                    {
                        TaskId = record.Id,
                        dependency.DependsOnId,
                        dependency.Type,
                        dependency.CreatedAt,
                        dependency.CreatedBy,
                        cancellationToken
                    },
                    transaction);
            }

            await connection.ExecuteAsync(
                "DELETE FROM comments WHERE task_id = @TaskId",
                new { TaskId = record.Id, cancellationToken },
                transaction);

            foreach (var comment in record.Comments)
            {
                // comments.id is a database-global AUTOINCREMENT counter, so
                // the same numeric id can be assigned independently by two
                // clones to comments on different tasks. Reusing the
                // exported id verbatim would collide with an unrelated
                // comment already occupying that id in this database (from
                // an earlier record in this same import or from a task
                // outside the imported set); detect that case and let
                // SQLite assign a fresh id instead of failing the import.
                var idOwner = await connection.ExecuteScalarAsync<string?>(
                    "SELECT task_id FROM comments WHERE id = @Id",
                    new { comment.Id, cancellationToken },
                    transaction);

                if (idOwner is null || idOwner == record.Id)
                {
                    await connection.ExecuteAsync(
                        "INSERT INTO comments (id, task_id, author, text, created_at) "
                        + "VALUES (@Id, @TaskId, @Author, @Text, @CreatedAt)",
                        new
                        {
                            comment.Id,
                            TaskId = record.Id,
                            comment.Author,
                            comment.Text,
                            comment.CreatedAt,
                            cancellationToken
                        },
                        transaction);
                }
                else
                {
                    await connection.ExecuteAsync(
                        "INSERT INTO comments (task_id, author, text, created_at) "
                        + "VALUES (@TaskId, @Author, @Text, @CreatedAt)",
                        new
                        {
                            TaskId = record.Id,
                            comment.Author,
                            comment.Text,
                            comment.CreatedAt,
                            cancellationToken
                        },
                        transaction);
                }
            }

            applied++;
        }

        await RestoreChildCountersAsync(connection, transaction, cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return new TaskImportResult(applied, skipped, records.Count);
    }

    // Import writes task rows directly and never calls CreateTaskIdAsync, so
    // child_counters is never populated for the imported tasks. Without this,
    // the next child created for an imported parent collides with a child id
    // that already exists (e.g. after "flush, delete db, import", creating a
    // second child for the same parent reuses ".1"). Rebuild each parent's
    // counter from the highest numeric child suffix present in the tasks
    // table, never lowering a counter that is already ahead.
    private static async Task RestoreChildCountersAsync(
        SqliteConnection connection,
        DbTransaction transaction,
        CancellationToken cancellationToken)
    {
        var taskIds = await connection.QueryAsync<string>(
            "SELECT id FROM tasks",
            transaction: transaction);

        var maxChildByParent = new Dictionary<string, long>();

        foreach (var id in taskIds)
        {
            var dotIndex = id.LastIndexOf('.');

            if (dotIndex <= 0
                || !long.TryParse(
                    id.AsSpan(dotIndex + 1),
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out var childNumber))
            {
                continue;
            }

            var parentId = id[..dotIndex];

            if (!maxChildByParent.TryGetValue(parentId, out var current) || childNumber > current)
            {
                maxChildByParent[parentId] = childNumber;
            }
        }

        foreach (var (parentId, lastChild) in maxChildByParent)
        {
            await connection.ExecuteAsync(
                "INSERT INTO child_counters (parent_id, last_child) VALUES (@parentId, @lastChild) "
                + "ON CONFLICT (parent_id) DO UPDATE SET last_child = MAX(last_child, excluded.last_child)",
                new { parentId, lastChild, cancellationToken },
                transaction);
        }
    }

    public async Task<TaskIntegrityReport> CheckIntegrityAsync(
        CancellationToken cancellationToken)
    {
        // Every query below except the tombstoned-parent-edge check runs
        // against a whole table and takes no filter parameters, so there is
        // nothing for @-placeholder analysis to key on; cancellation is
        // checked up front instead of plumbed through a parameter object
        // (see ComputeBlockedAsync).
        cancellationToken.ThrowIfCancellationRequested();

        await using var connection = await ConnectAsync(cancellationToken);

        var quickCheckMessage = await connection.ExecuteScalarAsync<string?>("PRAGMA quick_check;")
            ?? "ok";

        var orphanDependencies = (await connection.QueryAsync<DependencyEdgeRow>(
                "SELECT d.task_id AS TaskId, d.depends_on_id AS DependsOnId, "
                + "d.dependency_type AS Type FROM dependencies d "
                + "WHERE NOT EXISTS (SELECT 1 FROM tasks t WHERE t.id = d.task_id) "
                + "OR NOT EXISTS (SELECT 1 FROM tasks t WHERE t.id = d.depends_on_id) "
                + "ORDER BY d.task_id, d.depends_on_id"))
            .Select(row => new TaskDependencyReference(row.TaskId, row.DependsOnId))
            .ToList();

        var orphanLabels = (await connection.QueryAsync<TaskLabelRow>(
                "SELECT l.task_id AS TaskId, l.label AS Label FROM labels l "
                + "WHERE NOT EXISTS (SELECT 1 FROM tasks t WHERE t.id = l.task_id) "
                + "ORDER BY l.task_id, l.label"))
            .Select(row => new TaskOrphanLabel(row.TaskId, row.Label))
            .ToList();

        var orphanComments = (await connection.QueryAsync<CommentOrphanRow>(
                "SELECT c.task_id AS TaskId, c.id AS CommentId FROM comments c "
                + "WHERE NOT EXISTS (SELECT 1 FROM tasks t WHERE t.id = c.task_id) "
                + "ORDER BY c.task_id, c.id"))
            .Select(row => new TaskOrphanComment(row.TaskId, row.CommentId))
            .ToList();

        var tombstonedParentEdges = (await connection.QueryAsync<DependencyEdgeRow>(
                "SELECT d.task_id AS TaskId, d.depends_on_id AS DependsOnId, "
                + "d.dependency_type AS Type FROM dependencies d "
                + "JOIN tasks t ON t.id = d.depends_on_id "
                + "WHERE d.dependency_type = @parentChild AND t.status = @tombstone "
                + "ORDER BY d.task_id",
                new
                {
                    parentChild = TaskDependencyTypes.ParentChild,
                    tombstone = TaskStates.Tombstone,
                    cancellationToken
                }))
            .Select(row => new TaskDependencyReference(row.TaskId, row.DependsOnId))
            .ToList();

        return new TaskIntegrityReport
        {
            QuickCheckOk = quickCheckMessage == "ok",
            QuickCheckMessage = quickCheckMessage,
            OrphanDependencies = orphanDependencies,
            OrphanLabels = orphanLabels,
            OrphanComments = orphanComments,
            TombstonedParentEdges = tombstonedParentEdges
        };
    }

    // Searches the blocking-dependency graph for a path from dependsOnId
    // back to id. Combined with the edge just inserted (id -> dependsOnId),
    // such a path closes a cycle. Runs inside the same transaction as the
    // insert so the check sees a consistent snapshot.
    private static async Task<List<string>?> FindBlockingCycleAsync(
        SqliteConnection connection,
        DbTransaction transaction,
        string id,
        string dependsOnId)
    {
        var edges = (await connection.QueryAsync<DependencyEdgeRow>(
            "SELECT task_id AS TaskId, depends_on_id AS DependsOnId, dependency_type AS Type "
            + "FROM dependencies",
            transaction: transaction))
            .Where(e => TaskDependencyTypes.IsBlocking(e.Type))
            .ToList();

        var adjacency = edges
            .GroupBy(e => e.TaskId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.DependsOnId).OrderBy(x => x, StringComparer.Ordinal).ToList());

        var predecessor = new Dictionary<string, string?> { [dependsOnId] = null };
        var queue = new Queue<string>();
        queue.Enqueue(dependsOnId);

        while (queue.TryDequeue(out var current))
        {
            if (current == id)
            {
                var path = new List<string>();
                string? node = current;

                while (node is not null)
                {
                    path.Add(node);
                    node = predecessor[node];
                }

                path.Reverse();

                // path runs dependsOnId..id; drop the trailing id (already the
                // list's head) and close the loop by repeating the dependent
                // task at the end, so the cycle both starts and ends at id.
                var cycle = new List<string> { id };
                cycle.AddRange(path.Take(path.Count - 1));
                cycle.Add(id);

                return cycle;
            }

            if (!adjacency.TryGetValue(current, out var neighbors))
            {
                continue;
            }

            foreach (var next in neighbors)
            {
                if (!predecessor.ContainsKey(next))
                {
                    predecessor[next] = current;
                    queue.Enqueue(next);
                }
            }
        }

        return null;
    }

    // Builds a plain ADO.NET-ready SQL fragment and parameter map. The
    // "statuses" filter expands its own IN-list placeholders (rather than
    // relying on Dapper's array-parameter rewriting) because the resulting
    // query executes through plain ADO.NET, not Dapper: see the comment on
    // ExecuteTaskQueryAsync.
    private static (string WhereClause, Dictionary<string, object?> Parameters) BuildTaskFilterClause(
        TaskFilter filter)
    {
        var conditions = new List<string>();
        var parameters = new Dictionary<string, object?>();

        if (filter.Statuses is { Length: > 0 })
        {
            var placeholders = new string[filter.Statuses.Length];

            for (var i = 0; i < filter.Statuses.Length; i++)
            {
                var parameterName = $"status{i}";
                parameters[parameterName] = TaskStates.Normalize(filter.Statuses[i]);
                placeholders[i] = $"@{parameterName}";
            }

            conditions.Add($"status IN ({string.Join(", ", placeholders)})");
        }
        else if (!filter.IncludeAll)
        {
            parameters["closedStatus"] = TaskStates.Closed;
            parameters["tombstoneStatus"] = TaskStates.Tombstone;
            conditions.Add("status NOT IN (@closedStatus, @tombstoneStatus)");
        }

        if (filter.ExcludeTombstones)
        {
            parameters["tombstone"] = TaskStates.Tombstone;
            conditions.Add("status != @tombstone");
        }

        if (!string.IsNullOrEmpty(filter.Type))
        {
            parameters["type"] = TaskTypes.Normalize(filter.Type);
            conditions.Add("task_type = @type");
        }

        if (filter.Priority is { } priority)
        {
            parameters["priority"] = priority;
            conditions.Add("priority = @priority");
        }

        if (filter.PriorityMin is { } priorityMin && filter.PriorityMax is { } priorityMax)
        {
            parameters["priorityMin"] = priorityMin;
            parameters["priorityMax"] = priorityMax;
            conditions.Add("priority BETWEEN @priorityMin AND @priorityMax");
        }

        if (filter.Unassigned)
        {
            conditions.Add("(assignee IS NULL OR assignee = '')");
        }
        else if (!string.IsNullOrEmpty(filter.Assignee))
        {
            parameters["assignee"] = filter.Assignee;
            conditions.Add("assignee = @assignee");
        }

        if (filter.Labels is { Length: > 0 })
        {
            for (var i = 0; i < filter.Labels.Length; i++)
            {
                var parameterName = $"label{i}";
                parameters[parameterName] = filter.Labels[i].Trim().ToLowerInvariant();
                conditions.Add(
                    "EXISTS (SELECT 1 FROM labels WHERE task_id = tasks.id "
                    + $"AND label = @{parameterName})");
            }
        }

        if (!string.IsNullOrEmpty(filter.Text))
        {
            parameters["text"] = EscapeLikeText(filter.Text);
            conditions.Add(
                "(LOWER(title) LIKE '%' || LOWER(@text) || '%' ESCAPE '\\' OR "
                + "LOWER(description) LIKE '%' || LOWER(@text) || '%' ESCAPE '\\' OR "
                + "LOWER(design) LIKE '%' || LOWER(@text) || '%' ESCAPE '\\' OR "
                + "LOWER(acceptance_criteria) LIKE '%' || LOWER(@text) || '%' ESCAPE '\\' OR "
                + "LOWER(notes) LIKE '%' || LOWER(@text) || '%' ESCAPE '\\' OR "
                + "EXISTS (SELECT 1 FROM comments WHERE task_id = tasks.id "
                + "AND LOWER(text) LIKE '%' || LOWER(@text) || '%' ESCAPE '\\'))");
        }

        if (filter.UpdatedBefore is { } updatedBefore)
        {
            parameters["updatedBefore"] = updatedBefore;
            conditions.Add("updated_at <= @updatedBefore");
        }

        if (filter.DeferredVisibleAt is { } visibleAt)
        {
            parameters["visibleAt"] = visibleAt;
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

    // These nested row types are internal, not private: Dapper.AOT's
    // generated interceptors live outside TaskStore and cannot reference a
    // private nested type, so a private row type would silently fall back to
    // Dapper's reflection-emit deserializer.
    internal sealed class TaskRow
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

    /// <summary>
    /// Column ordinals for <see cref="TaskRow"/>, captured once per reader
    /// and reused for every row so <see cref="ExecuteTaskQueryAsync"/> reads
    /// plain ADO.NET fields without any per-row reflection.
    /// </summary>
    private readonly struct TaskRowColumns(
        int id, int title, int description, int design, int acceptanceCriteria, int notes,
        int status, int priority, int type, int assignee, int estimatedMinutes, int dueAt,
        int deferUntil, int createdAt, int createdBy, int updatedAt, int closedAt,
        int closeReason, int deletedAt, int deleteReason)
    {
        public static TaskRowColumns From(DbDataReader reader) => new(
            reader.GetOrdinal("Id"),
            reader.GetOrdinal("Title"),
            reader.GetOrdinal("Description"),
            reader.GetOrdinal("Design"),
            reader.GetOrdinal("AcceptanceCriteria"),
            reader.GetOrdinal("Notes"),
            reader.GetOrdinal("Status"),
            reader.GetOrdinal("Priority"),
            reader.GetOrdinal("Type"),
            reader.GetOrdinal("Assignee"),
            reader.GetOrdinal("EstimatedMinutes"),
            reader.GetOrdinal("DueAt"),
            reader.GetOrdinal("DeferUntil"),
            reader.GetOrdinal("CreatedAt"),
            reader.GetOrdinal("CreatedBy"),
            reader.GetOrdinal("UpdatedAt"),
            reader.GetOrdinal("ClosedAt"),
            reader.GetOrdinal("CloseReason"),
            reader.GetOrdinal("DeletedAt"),
            reader.GetOrdinal("DeleteReason"));

        public TaskRow Read(DbDataReader reader) => new()
        {
            Id = reader.GetString(id),
            Title = reader.GetString(title),
            Description = reader.GetString(description),
            Design = reader.GetString(design),
            AcceptanceCriteria = reader.GetString(acceptanceCriteria),
            Notes = reader.GetString(notes),
            Status = reader.GetString(status),
            Priority = reader.GetInt32(priority),
            Type = reader.GetString(type),
            Assignee = reader.IsDBNull(assignee) ? null : reader.GetString(assignee),
            EstimatedMinutes = reader.IsDBNull(estimatedMinutes)
                ? null
                : reader.GetInt32(estimatedMinutes),
            DueAt = reader.IsDBNull(dueAt) ? null : reader.GetString(dueAt),
            DeferUntil = reader.IsDBNull(deferUntil) ? null : reader.GetString(deferUntil),
            CreatedAt = reader.GetString(createdAt),
            CreatedBy = reader.GetString(createdBy),
            UpdatedAt = reader.GetString(updatedAt),
            ClosedAt = reader.IsDBNull(closedAt) ? null : reader.GetString(closedAt),
            CloseReason = reader.GetString(closeReason),
            DeletedAt = reader.IsDBNull(deletedAt) ? null : reader.GetString(deletedAt),
            DeleteReason = reader.GetString(deleteReason)
        };
    }

    internal sealed class TaskGraphNode
    {
        public required string Id { get; init; }
        public required string Status { get; init; }
        public required string Type { get; init; }
    }

    internal sealed class TaskGraphEdge
    {
        public required string TaskId { get; init; }
        public required string DependsOnId { get; init; }
        public required string Type { get; init; }
    }

    /// <summary>
    /// A group-by-priority row, kept numeric so it can be formatted as
    /// P0..P4.
    /// </summary>
    internal sealed class PriorityCountRow
    {
        public required int Priority { get; init; }
        public required int Count { get; init; }
    }

    /// <summary>
    /// One group-by-value row: the grouped value and how many tasks fall
    /// into it.
    /// </summary>
    internal sealed class CountRow
    {
        public required string Value { get; init; }
        public required int Count { get; init; }
    }

    /// <summary>
    /// A label's name and how many non-tombstone tasks carry it.
    /// </summary>
    internal sealed class LabelCountRow
    {
        public required string Label { get; init; }
        public required int Count { get; init; }
    }

    /// <summary>
    /// A dependency edge's task pair and type, for the blocking-cycle walk in
    /// <see cref="AddDependencyAsync"/> and the integrity checks in
    /// <see cref="CheckIntegrityAsync"/>.
    /// </summary>
    internal sealed class DependencyEdgeRow
    {
        public required string TaskId { get; init; }
        public required string DependsOnId { get; init; }
        public required string Type { get; init; }
    }

    /// <summary>
    /// A comment's task ID and row ID, for the orphan-comment check in
    /// <see cref="CheckIntegrityAsync"/>.
    /// </summary>
    internal sealed class CommentOrphanRow
    {
        public required string TaskId { get; init; }
        public required long CommentId { get; init; }
    }

    /// <summary>
    /// A task-label pair, for grouping labels by task in
    /// <see cref="ExportTasksAsync"/>.
    /// </summary>
    internal sealed class TaskLabelRow
    {
        public required string TaskId { get; init; }
        public required string Label { get; init; }
    }
}
