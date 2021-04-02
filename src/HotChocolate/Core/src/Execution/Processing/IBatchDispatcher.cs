using System;

namespace HotChocolate.Execution.Processing
{
    internal interface IBatchDispatcher
    {
        void Register(IExecutionContext context);

        void Unregister(IExecutionContext context);
    }
}
