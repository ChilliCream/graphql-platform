using Microsoft.CodeAnalysis;
using static System.IO.Path;

namespace HotChocolate.Analyzers
{
    public static class Directories
    {
        public static string GetBinDirectory(this GeneratorExecutionContext context)
        {
            if (context.AnalyzerConfigOptions.GlobalOptions.TryGetValue(
                "build_property.HotChocolate_BinDir",
                out var value) &&
                !string.IsNullOrEmpty(value))
            {
                return value.Replace(AltDirectorySeparatorChar, DirectorySeparatorChar);
            }

            return GetDirectoryName(typeof(Directories).Assembly.Location)!;
        }
    }
}
