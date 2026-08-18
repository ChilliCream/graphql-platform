using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Tasks;
using Dapper;

namespace ChilliCream.Nitro.CommandLine.Commands.Tasks;

internal sealed class ListTaskConfigCommand : Command
{
    public ListTaskConfigCommand() : base("list")
    {
        Description = "List all configuration values.";

        this.AddExamples("task config list");

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static async Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken cancellationToken)
    {
        var console = services.GetRequiredService<INitroConsole>();
        var store = services.GetRequiredService<ITaskStore>();

        await using var connection = await store.ConnectAsync(cancellationToken);

        var rows = await connection.QueryAsync<ConfigRow>(
            "SELECT key AS Key, value AS Value FROM config ORDER BY key ASC");

        foreach (var row in rows)
        {
            console.WriteLine($"{row.Key} = {row.Value}");
        }

        return ExitCodes.Success;
    }

    /// <summary>
    /// One row of the config table.
    /// </summary>
    private sealed class ConfigRow
    {
        public required string Key { get; init; }
        public required string Value { get; init; }
    }
}
