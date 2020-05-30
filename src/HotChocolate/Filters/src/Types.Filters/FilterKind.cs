namespace HotChocolate.Types.Filters
{
    public static class FilterKind
    {
        /// <summary>
        /// Ignored Filters
        /// </summary>
        public const string Ignored = "Ignored";

        /// <summary>
        /// All String Filters
        /// </summary>
        public const string String = "String";

        /// <summary>
        /// All Comparable Filters
        /// </summary>
        public const string Comparable = "Comparable";

        /// <summary>
        /// All Boolean Filters
        /// </summary>
        public const string Boolean = "Boolean";

        /// <summary>
        /// All Array Filters
        /// </summary>
        public const string Array = "Array";

        /// <summary>
        /// All Object Filters
        /// </summary>
        public const string Object = "Object";
    }
}
