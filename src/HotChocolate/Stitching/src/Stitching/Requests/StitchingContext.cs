using System.Globalization;
using GreenDonut;
using HotChocolate.Execution;
using HotChocolate.Stitching.Properties;
using HotChocolate.Utilities;

namespace HotChocolate.Stitching.Requests;

public class StitchingContext : IStitchingContext
{
    private readonly Dictionary<string, RemoteRequestExecutor> _executors = new();

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

        foreach (var executor in
            requestContextAccessor.RequestContext.Schema.GetRemoteExecutors())
        {
            _executors.Add(
                executor.Key,
                new RemoteRequestExecutor(
                    batchScheduler,
                    executor.Value));
        }
    }

    public IRemoteRequestExecutor GetRemoteRequestExecutor(string schemaName)
    {
        schemaName.EnsureGraphQLName(nameof(schemaName));

        if (_executors.TryGetValue(schemaName, out var executor))
        {
            return executor;
        }

        throw new ArgumentException(string.Format(
            CultureInfo.InvariantCulture,
            StitchingResources.SchemaName_NotFound,
            schemaName));
    }

    public ISchema GetRemoteSchema(string schemaName) =>
        GetRemoteRequestExecutor(schemaName).Schema;
}
