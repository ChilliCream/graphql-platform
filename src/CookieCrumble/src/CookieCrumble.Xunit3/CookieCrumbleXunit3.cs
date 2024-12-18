namespace CookieCrumble.Xunit3;

public static class CookieCrumbleXunit3
{
    public static void Initialize()
    {
        Snapshot.RegisterTestFramework(new Xunit3Framework());
    }
}
