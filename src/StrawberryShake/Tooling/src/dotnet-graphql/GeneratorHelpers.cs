namespace StrawberryShake.Tools;

public static class GeneratorHelpers
{
    public static string GetCodeGenServerLocation()
    {
        var s = Path.Combine(
            Path.GetDirectoryName(typeof(GeneratorHelpers).Assembly.Location)!,
            "..", "..", "..", "build", "gen", "BerryCodeGen.dll");
        Console.WriteLine(s);
        return s;
    }

    public static string[] GetConfigFiles(string path)
        => Directory.GetFiles(path, ".graphqlrc.json", SearchOption.AllDirectories);

    public static string[] GetDocuments(string path)
        => Directory.GetFiles(path, "*.graphql", SearchOption.AllDirectories);
}
