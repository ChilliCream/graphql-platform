using System.CommandLine.Help;
using System.Runtime.CompilerServices;

namespace ChilliCream.Nitro.CommandLine.Helpers;

internal static class CommandExamples
{
    private static readonly ConditionalWeakTable<Command, string[]> s_examples = [];

    public static void AddExamples(Command command, string[] examples)
    {
        s_examples.AddOrUpdate(command, examples);
    }

    public static bool TryGetExamples(Command command, out string[]? examples)
    {
        return s_examples.TryGetValue(command, out examples);
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
