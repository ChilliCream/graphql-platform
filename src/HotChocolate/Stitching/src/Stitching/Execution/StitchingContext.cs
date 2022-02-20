using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using GreenDonut;
using HotChocolate.Execution;
using HotChocolate.Stitching.Properties;

namespace HotChocolate.Stitching.Execution;

public class StitchingContext : IStitchingContext
{
    private readonly Dictionary<NameString, RemoteRequestScheduler> _executors = new();

    public StitchingContext(
        IBatchScheduler batchScheduler,
        IRequestContextAccessor requestContextAccessor)
    {
        if (batchScheduler is null)
        {
            throw new ArgumentNullException(nameof(batchScheduler));
        }

        if (requestContextAccessor is null)
        {
            throw new ArgumentNullException(nameof(requestContextAccessor));
        }

        foreach (KeyValuePair<NameString, IRequestExecutor> executor in
            requestContextAccessor.RequestContext.Schema.GetRemoteExecutors())
        {
            _executors.Add(executor.Key, new(batchScheduler, executor.Value));
        }
    }

    public ISchema GetRemoteSchema(NameString schemaName)
        => GetRemoteRequestScheduler(schemaName).Schema;

    public Task<IExecutionResult> ScheduleRequestAsync(
        NameString schemaName,
        IQueryRequest request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        IRemoteRequestScheduler scheduler = GetRemoteRequestScheduler(schemaName);
        return scheduler.ScheduleAsync(request, cancellationToken);
    }

    public Task<IExecutionResult> ExecuteRequestAsync(
        NameString schemaName,
        IQueryRequest request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        IRemoteRequestScheduler scheduler = GetRemoteRequestScheduler(schemaName);
        return scheduler.Executor.ExecuteAsync(request, cancellationToken);
    }

    private IRemoteRequestScheduler GetRemoteRequestScheduler(NameString schemaName)
    {
        schemaName.EnsureNotEmpty(nameof(schemaName));

        if (_executors.TryGetValue(schemaName, out RemoteRequestScheduler? executor))
        {
            return executor;
        }

        throw new ArgumentException(string.Format(
            CultureInfo.InvariantCulture,
            StitchingResources.SchemaName_NotFound,
            schemaName));
    }
}
