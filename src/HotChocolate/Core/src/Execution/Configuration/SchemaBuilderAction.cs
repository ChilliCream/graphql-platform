using System;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Execution.Configuration
{
    public readonly struct SchemaBuilderAction
    {
        public SchemaBuilderAction(
            Action<ISchemaBuilder> action)
        {
            Action = action ?? throw new ArgumentNullException(nameof(action));
            AsyncAction = default;
        }

        public SchemaBuilderAction(
            Func<ISchemaBuilder, CancellationToken, ValueTask> asyncAction)
        {
            Action = default;
            AsyncAction = asyncAction ?? throw new ArgumentNullException(nameof(asyncAction));
        }

        public Action<ISchemaBuilder>? Action { get; }

        public Func<ISchemaBuilder, CancellationToken, ValueTask>? AsyncAction { get; }
    }
}
