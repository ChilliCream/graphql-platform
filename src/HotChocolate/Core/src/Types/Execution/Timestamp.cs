using System.Diagnostics;

namespace HotChocolate;

internal static class Timestamp
{
    private const long NanosecondsPerSecond = 1000000000;

    public static long GetNowInNanoseconds()
    {
        return Stopwatch.GetTimestamp()
            * (NanosecondsPerSecond / Stopwatch.Frequency);
    }
}
