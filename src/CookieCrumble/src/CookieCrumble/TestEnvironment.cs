namespace CookieCrumble;

/// <summary>
/// Provides constant that help create specific snapshots for specific environments.
/// </summary>
public static class TestEnvironment
{
#if NET6_0
    /// <summary>
    /// The target framework identifier.
    /// </summary>
    public const string TargetFramework = "NET6_0";
#elif NET7_0
    /// <summary>
    /// The target framework identifier.
    /// </summary>
    public const string TargetFramework = "NET7_0";
#elif NET8_0
    /// <summary>
    /// The target framework identifier.
    /// </summary>
    public const string TargetFramework = "NET8_0";
#endif

    public static bool IsLocalEnvironment()
    {
        return !IsCIEnvironment();
    }

    public static bool IsCIEnvironment()
    {
        return bool.TryParse(
            Environment.GetEnvironmentVariable("CI_BUILD"),
            out var result) &&
            result;
    }
}
