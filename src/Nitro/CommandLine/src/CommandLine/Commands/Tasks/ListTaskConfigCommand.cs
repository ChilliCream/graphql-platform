using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Tasks;

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

        var rows = await store.ListConfigAsync(cancellationToken);

        foreach (var row in rows)
        {
            console.WriteLine($"{row.Key} = {row.Value}");
        }

        return ExitCodes.Success;
    }
}
