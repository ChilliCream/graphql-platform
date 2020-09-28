using System;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution.Options;

namespace HotChocolate.Execution.Configuration
{
    public readonly struct RequestExecutorOptionsAction
    {
        public RequestExecutorOptionsAction(Action<RequestExecutorOptions> action)
        {
            Action = action;
            AsyncAction = default;
        }

        public RequestExecutorOptionsAction(
            Func<RequestExecutorOptions, CancellationToken, ValueTask> asyncAction)
        {
            Action = default;
            AsyncAction = asyncAction;
        }

        public Action<RequestExecutorOptions>? Action { get; }

        public Func<RequestExecutorOptions, CancellationToken, ValueTask>? AsyncAction { get; }
    }
}