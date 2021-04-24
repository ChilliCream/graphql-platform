using System;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution.Options;

namespace HotChocolate.Execution.Configuration
{
    public readonly struct RequestExecutorOptionsAction
    {
        public RequestExecutorOptionsAction(Action<RequestExecutorAnalyzerOptions> action)
        {
            Action = action;
            AsyncAction = default;
        }

        public RequestExecutorOptionsAction(
            Func<RequestExecutorAnalyzerOptions, CancellationToken, ValueTask> asyncAction)
        {
            Action = default;
            AsyncAction = asyncAction;
        }

        public Action<RequestExecutorAnalyzerOptions>? Action { get; }

        public Func<RequestExecutorAnalyzerOptions, CancellationToken, ValueTask>? AsyncAction { get; }
    }
}