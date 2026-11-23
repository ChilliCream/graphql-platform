using System.Net;
using System.Text;
using HotChocolate.Fusion.Diagnostics;
using HotChocolate.Fusion.Execution;
using HotChocolate.Fusion.Execution.Nodes;
using Microsoft.AspNetCore.TestHost;

namespace HotChocolate.Fusion;

public partial class SubgraphErrorTests
{
    private TestServer CreateOrderedSubgraph(string name, string schema, string payload, SourceResultOrder order)
        => CreateSourceSchema(name, schema, mockHttpResponse: async _ =>
        {
            await order.WaitForTurnAsync(name);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json")
            };
        });

    private sealed class SourceResultOrder : FusionExecutionDiagnosticEventListener
    {
        private readonly Dictionary<string, Step> _steps = [];

        public SourceResultOrder(string order)
        {
            var previous = Task.CompletedTask;
            foreach (var name in order)
            {
                var step = new Step(previous);
                _steps.Add(name.ToString(), step);
                previous = step.Completed.Task;
            }
        }

        public Task WaitForTurnAsync(string name)
            => _steps[name].Previous.WaitAsync(TimeSpan.FromSeconds(30), TestContext.Current.CancellationToken);

        public override IDisposable ExecuteOperationNode(
            OperationPlanContext context,
            OperationExecutionNode node,
            string schemaName)
            => _steps[schemaName];

        // The next response is released after this node has enqueued its result for merging.
        private sealed class Step(Task previous) : IDisposable
        {
            public Task Previous { get; } = previous;

            public TaskCompletionSource Completed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

            public void Dispose() => Completed.TrySetResult();
        }
    }
}
