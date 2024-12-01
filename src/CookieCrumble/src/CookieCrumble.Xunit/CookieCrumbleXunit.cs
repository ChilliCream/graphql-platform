namespace CookieCrumble.Xunit;

public static class CookieCrumbleXunit
{
    public static void Initialize()
    {
        Snapshot.RegisterTestFramework(new XunitFramework());
    }
}
