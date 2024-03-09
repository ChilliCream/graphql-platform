using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HotChocolate.Execution;

public sealed class BatchOperationResult(
    IReadOnlyList<IOperationResult> results,
    IReadOnlyDictionary<string, object?>? contextData = null)
    : ExecutionResult(cleanupTasks: [() => RunCleanUp(results),])
{
    public IReadOnlyList<IOperationResult> Results { get; } = 
        results ?? throw new ArgumentNullException(nameof(results));

    public override ExecutionResultKind Kind => ExecutionResultKind.BatchResult;

    public override IReadOnlyDictionary<string, object?>? ContextData { get; } = 
        contextData;

    private static async ValueTask RunCleanUp(IReadOnlyList<IOperationResult> results)
    {
        foreach (var result in results)
        {
            await result.DisposeAsync().ConfigureAwait(false);
        }
    }
}