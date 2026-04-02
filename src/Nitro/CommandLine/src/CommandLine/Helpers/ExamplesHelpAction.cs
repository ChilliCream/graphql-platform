using System.CommandLine.Help;
using System.CommandLine.Invocation;
using System.Runtime.CompilerServices;

namespace ChilliCream.Nitro.CommandLine.Helpers;

internal static class CommandExamples
{
    private static readonly ConditionalWeakTable<Command, string[]> _examples = new();

    public static void AddExamples(Command command, string[] examples)
    {
        _examples.AddOrUpdate(command, examples);
    }

    public static bool TryGetExamples(Command command, out string[]? examples)
    {
        return _examples.TryGetValue(command, out examples);
    }

    public static void Install(RootCommand rootCommand)
    {
        for (var i = 0; i < rootCommand.Options.Count; i++)
        {
            if (rootCommand.Options[i] is HelpOption helpOption
                && helpOption.Action is HelpAction helpAction)
            {
                helpOption.Action = new ExamplesHelpAction(helpAction);
                return;
            }
        }
    }
}

internal sealed class ExamplesHelpAction : SynchronousCommandLineAction
{
    private readonly HelpAction _defaultHelp;

    public ExamplesHelpAction(HelpAction defaultHelp)
    {
        _defaultHelp = defaultHelp;
    }

    public override int Invoke(ParseResult parseResult)
    {
        var result = _defaultHelp.Invoke(parseResult);

        var command = parseResult.CommandResult.Command;

        if (CommandExamples.TryGetExamples(command, out var examples) && examples is not null)
        {
            var console = CommandExecutionContext.Services.Value
                ?.GetRequiredService<INitroConsole>();

            if (console is not null)
            {
                var rootName = parseResult.RootCommandResult.Command.Name;

                console.WriteLine("Example:");

                foreach (var example in examples)
                {
                    var lines = example.Split('\n');

                    for (var i = 0; i < lines.Length; i++)
                    {
                        if (i == 0)
                        {
                            console.WriteLine($"  {rootName} {lines[i]}");
                        }
                        else
                        {
                            console.WriteLine($"  {lines[i]}");
                        }
                    }
                }

                console.WriteLine();
            }
        }

        return result;
    }
}
