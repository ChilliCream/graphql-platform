namespace StrawberryShake.CodeGeneration.CSharp;

public static class GeneratorSink
{
    public static string Location => Path.Combine(Path.GetTempPath(), "__berry");

    public static string ErrorLogFileName => Path.Combine(Location, "errors.log");

    public static string CreateFileName() => Path.Combine(Location, Path.GetRandomFileName());
}
