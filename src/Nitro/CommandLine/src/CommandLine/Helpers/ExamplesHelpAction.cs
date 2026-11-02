using System.CommandLine.Help;
using System.CommandLine.Invocation;

namespace ChilliCream.Nitro.CommandLine.Helpers;

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
            var console = CommandExecutionContext.TryGetServices()
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
