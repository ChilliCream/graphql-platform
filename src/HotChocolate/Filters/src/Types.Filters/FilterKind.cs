namespace HotChocolate.Types.Filters
{
    public static class FilterKind
    {
        /// <summary>
        /// Ignored Filters
        /// </summary>
        public const int Ignored = 1;

        /// <summary>
        /// All String Filters
        /// </summary>
        public const int String = 2;

        /// <summary>
        /// All Comparable Filters
        /// </summary>
        public const int Comparable = 3;

        /// <summary>
        /// All Boolean Filters
        /// </summary>
        public const int Boolean = 4;

        /// <summary>
        /// All Array Filters
        /// </summary>
        public const int Array = 21;

        /// <summary>
        /// All Object Filters
        /// </summary>
        public const int Object = 6;

        public const int Custom = 24;

        public const int Skip = 25;
    }
}
