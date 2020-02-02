using System;

namespace GreenDonut
{
    internal static class Defaults
    {
        public const int CacheSize = 1000;
        public static readonly TimeSpan BatchRequestDelay = TimeSpan
            .FromMilliseconds(50);
        public const int MinimumCacheSize = 1;
        public static readonly TimeSpan SlidingExpiration = TimeSpan.Zero;
    }
}
