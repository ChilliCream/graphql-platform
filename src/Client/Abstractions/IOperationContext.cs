using System.Collections.Generic;
using System.Threading;

namespace StrawberryShake
{
    public interface IOperationContext
    {
        IOperation Operation { get; }

        IOperationResultBuilder Result { get; }

        IResultParser ResultParser { get; }

        IDictionary<string, object> ContextData { get; }

        CancellationToken RequestAborted { get; }
    }
}
