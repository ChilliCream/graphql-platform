using System.Diagnostics;

namespace CookieCrumble;

/// <summary>
/// Provides constant that help create specific snapshots for specific environments.
/// </summary>
public static class TestEnvironment
{
#if NET8_0
    /// <summary>
    /// The target framework identifier.
    /// </summary>
    public const string TargetFramework = "NET8_0";
#elif NET9_0
    /// <summary>
    /// The target framework identifier.
    /// </summary>
    public const string TargetFramework = "NET9_0";
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

    public static CancellationTokenSource CreateCancellationTokenSource(TimeSpan? timeSpan = null)
    {
        if (Debugger.IsAttached)
        {
            return new CancellationTokenSource();
        }

        return new CancellationTokenSource(timeSpan ?? TimeSpan.FromSeconds(10));
    }
}
