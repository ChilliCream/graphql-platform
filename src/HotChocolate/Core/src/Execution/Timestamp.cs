using System.Diagnostics;

namespace HotChocolate
{
    internal static class Timestamp
    {
        private const long _nanosecondsPerSecond = 1000000000;

        public static long GetNowInNanoseconds()
        {
            return Stopwatch.GetTimestamp() *
                (_nanosecondsPerSecond / Stopwatch.Frequency);
        }
    }
}
