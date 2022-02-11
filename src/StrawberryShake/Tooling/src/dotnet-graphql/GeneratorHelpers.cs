using static System.IO.Directory;
using static System.IO.Path;
using static System.IO.SearchOption;

namespace StrawberryShake.Tools;

public static class GeneratorHelpers
{
    public static string GetCodeGenServerLocation()
        => Combine(
            GetDirectoryName(typeof(GeneratorHelpers).Assembly.Location)!,
            "..", "..", "..", "build", "gen", "BerryCodeGen.dll");

    public static string[] GetConfigFiles(string path)
        => GetFiles(path, ".graphqlrc.json", AllDirectories);

    public static string[] GetDocuments(string path)
        => GetFiles(path, "*.graphql", AllDirectories);
}
