namespace HotChocolate.Data.Filters.Spatial
{
    /// <summary>
    /// All of the available spatial operations
    /// </summary>
    public static class SpatialFilterOperations
    {
        public const int Buffer = 513;

        public const int Geometry = 514;

        public const int Contains = 515;
        public const int NotContains = 516;

        public const int Distance = 517;

        public const int Intersects = 518;
        public const int NotIntersects = 519;

        public const int Overlaps = 520;
        public const int NotOverlaps = 521;

        public const int Touches = 522;
        public const int NotTouches = 523;

        public const int Within = 524;
        public const int NotWithin = 525;
    }
}
