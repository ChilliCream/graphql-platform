using ChilliCream.Nitro.CommandLine.Commands.Tasks.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Tasks;

namespace ChilliCream.Nitro.CommandLine.Commands.Tasks;

internal sealed class GetTaskConfigCommand : Command
{
    public GetTaskConfigCommand() : base("get")
    {
        Description = "Get a configuration value.";

        Arguments.Add(Opt<ConfigKeyArgument>.Instance);

        this.AddExamples("task config get prefix");

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static async Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken cancellationToken)
    {
        var console = services.GetRequiredService<INitroConsole>();
        var store = services.GetRequiredService<ITaskStore>();

        var key = parseResult.GetRequiredValue(Opt<ConfigKeyArgument>.Instance);

        await using var connection = await store.ConnectAsync(cancellationToken);

        var value = await store.GetConfigAsync(connection, key, cancellationToken)
            ?? throw new ExitException($"Configuration key '{key}' is not set.");

        console.WriteLine(value);

        return ExitCodes.Success;
    }
}
