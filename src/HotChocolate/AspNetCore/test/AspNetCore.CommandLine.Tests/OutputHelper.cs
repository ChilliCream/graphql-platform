using System.Text.RegularExpressions;

namespace HotChocolate.AspNetCore.CommandLine;

internal static partial class OutputHelper
{
    public static string ReplaceExecutableName(string output)
    {
        return ExecutableNameRegex().Replace(output, "$1  graphql");
    }

    [GeneratedRegex(@"(Usage:\r?\n)  [\w]+")]
    private static partial Regex ExecutableNameRegex();
}
