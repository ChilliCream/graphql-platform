namespace CookieCrumble;

internal sealed class TestInfo
{
    internal string FileName { get; }
    internal TestType Type { get; }

    internal TestInfo(string fileName, TestType type)
    {
        FileName = fileName;
        Type = type;
    }
}
