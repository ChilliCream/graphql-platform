namespace HotChocolate.Types.Analyzers;

public static class TestLogger
{
    private const string _path =
        "/Users/michael/local/hc-3/src/HotChocolate/Core/test/Types.Analyzers.Tests/log.txt";

    public static void WriteLine(string line)
    {
        File.AppendAllText(_path, line + Environment.NewLine);
    }

}
