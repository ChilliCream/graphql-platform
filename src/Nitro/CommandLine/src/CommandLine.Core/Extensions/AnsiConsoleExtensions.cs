using System.CommandLine.IO;

namespace ChilliCream.Nitro.CommandLine;

internal static class AnsiConsoleExtensions
{
    extension(IAnsiConsole ansiConsole)
    {
        public TextWriter Out => Console.Out;

        public TextWriter Error => Console.Error;
    }
}
