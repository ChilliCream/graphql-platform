using ChilliCream.Nitro.CommandLine.Commands.Tasks.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Tasks;

namespace ChilliCream.Nitro.CommandLine.Commands.Tasks;

internal sealed class SetTaskConfigCommand : Command
{
    private const string PrefixKey = "prefix";

    public SetTaskConfigCommand() : base("set")
    {
        Description = "Set a configuration value.";

        Arguments.Add(Opt<ConfigKeyArgument>.Instance);
        Arguments.Add(Opt<ConfigValueArgument>.Instance);
        Options.Add(Opt<OptionalOutputFormatOption>.Instance);

        this.AddExamples("task config set prefix \"app\"");

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static async Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken cancellationToken)
    {
        var console = services.GetRequiredService<INitroConsole>();
        var store = services.GetRequiredService<ITaskStore>();
        var resultHolder = services.GetRequiredService<IResultHolder>();

        var key = parseResult.GetRequiredValue(Opt<ConfigKeyArgument>.Instance);
        var value = parseResult.GetRequiredValue(Opt<ConfigValueArgument>.Instance);

        if (key == PrefixKey)
        {
            value = TaskWorkspace.NormalizePrefix(value);
        }

        await store.SetConfigAsync(key, value, cancellationToken);

        if (!console.IsHumanReadable)
        {
            resultHolder.SetResult(new ObjectResult(new TaskConfigEntry(key, value)));
            return ExitCodes.Success;
        }

        console.OkLine($"Set '{key.EscapeMarkup()}' to '{value.EscapeMarkup()}'.");

        return ExitCodes.Success;
    }
}
