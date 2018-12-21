using System;

namespace HotChocolate.Execution.Configuration
{
    public interface IRequestTimeoutOptionsAccessor
    {
        TimeSpan ExecutionTimeout { get; }
    }
}
