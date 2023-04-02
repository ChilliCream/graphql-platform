using System;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Execution.Configuration;

public readonly struct OnRequestExecutorCreatedAction
{
    public OnRequestExecutorCreatedAction(OnRequestExecutorCreated action)
    {
        Created = action ?? throw new ArgumentNullException(nameof(action));
        CreatedAsync = default;
    }

    public OnRequestExecutorCreatedAction(OnRequestExecutorCreatedAsync asyncAction)
    {
        Created = default;
        CreatedAsync = asyncAction ?? throw new ArgumentNullException(nameof(asyncAction));
    }

    public OnRequestExecutorCreated? Created { get; }

    public OnRequestExecutorCreatedAsync? CreatedAsync { get; }
}

public delegate void OnRequestExecutorCreated(
    ConfigurationContext context,
    IRequestExecutor executor);

public delegate ValueTask OnRequestExecutorCreatedAsync(
    ConfigurationContext context,
    IRequestExecutor executor,
    CancellationToken cancellationToken);
