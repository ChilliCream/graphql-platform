using System;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Execution.Configuration
{
    public readonly struct SchemaBuilderAction
    {
        public SchemaBuilderAction(Action<SchemaBuilder> action)
        {
            Action = action;
            AsyncAction = default;
        }

        public SchemaBuilderAction(Func<SchemaBuilder, CancellationToken, ValueTask> asyncAction)
        {
            Action = default;
            AsyncAction = asyncAction;
        }

        public Action<SchemaBuilder>? Action { get; }

        public Func<SchemaBuilder, CancellationToken, ValueTask>? AsyncAction { get; }
    }
}