using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution;

namespace HotChocolate.Stitching.Execution;

public interface IStitchingContext
{
    ISchema GetRemoteSchema(NameString schemaName);

    Task<IExecutionResult> ScheduleRequestAsync(
        NameString schemaName,
        IQueryRequest request,
        CancellationToken cancellationToken);

    Task<IExecutionResult> ExecuteRequestAsync(
        NameString schemaName,
        IQueryRequest request,
        CancellationToken cancellationToken);
}
