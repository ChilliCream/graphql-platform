using McMaster.Extensions.CommandLineUtils;

namespace StrawberryShake.Tools;

internal static class Program
{
    internal static Task<int> Main(string[] args)
    {
        using var root = new CommandLineApplication();

        root.Description = "Strawberry Shake GraphQL Generator";

        root.HelpOption(inherited: true);
        root.Command("init", InitCommand.Build);
        root.Command("update", UpdateCommand.Build);
        root.Command("download", DownloadCommand.Build);
        root.Command("generate", GenerateCommand.Build);
        root.Command("where", WhereCommand.Build);

        root.OnExecute(() =>
        {
            root.ShowHelp();
            return 1;
        });

        return root.ExecuteAsync(args);
    }
}
