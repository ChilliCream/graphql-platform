using System;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Execution.Configuration
{
    public readonly struct SchemaBuilderAction
    {
        public SchemaBuilderAction(
            Action<IServiceProvider, ISchemaBuilder> action)
        {
            Action = action ?? throw new ArgumentNullException(nameof(action));
            AsyncAction = default;
        }

        public SchemaBuilderAction(
            Func<IServiceProvider, ISchemaBuilder, CancellationToken, ValueTask> asyncAction)
        {
            Action = default;
            AsyncAction = asyncAction ?? throw new ArgumentNullException(nameof(asyncAction));
        }

        public Action<IServiceProvider, ISchemaBuilder>? Action
        {
            get;
        }

        public Func<IServiceProvider, ISchemaBuilder, CancellationToken, ValueTask>? AsyncAction
        {
            get;
        }
    }
}
