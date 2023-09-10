using McMaster.Extensions.CommandLineUtils;

namespace StrawberryShake.Tools;

public static class WhereCommand
{
    public static void Build(CommandLineApplication where)
    {
        where.Description = "Get the tool location";

        where.OnExecute(() =>
        {
            Console.WriteLine($"Working Directory: {Environment.CurrentDirectory}");
            Console.WriteLine($"Location: {typeof(WhereCommand).Assembly.Location}");
            return 0;
        });
    }
}
