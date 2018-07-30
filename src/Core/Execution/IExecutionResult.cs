using System.Collections.Generic;

namespace HotChocolate.Execution
{
    public interface IExecutionResult
    {
        IReadOnlyCollection<IQueryError> Errors { get; }
    }
}
