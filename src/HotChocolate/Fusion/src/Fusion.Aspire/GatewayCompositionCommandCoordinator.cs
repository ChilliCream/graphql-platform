using Aspire.Hosting.ApplicationModel;

namespace HotChocolate.Fusion.Aspire;

internal sealed class GatewayCompositionCommandCoordinator
{
    private readonly Dictionary<string, Func<CancellationToken, Task<ExecuteCommandResult>>> _commands =
        [with(StringComparer.Ordinal)];
    private readonly Lock _sync = new();

    public void Register(
        string resourceName,
        Func<CancellationToken, Task<ExecuteCommandResult>> command)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceName);
        ArgumentNullException.ThrowIfNull(command);

        lock (_sync)
        {
            _commands[resourceName] = command;
        }
    }

    public Task<ExecuteCommandResult> ExecuteAsync(
        string resourceName,
        CancellationToken cancellationToken)
    {
        Func<CancellationToken, Task<ExecuteCommandResult>>? command;
        lock (_sync)
        {
            _commands.TryGetValue(resourceName, out command);
        }

        return command is null
            ? Task.FromResult(CommandResults.Failure("Schema composition is not ready."))
            : command(cancellationToken);
    }
}
