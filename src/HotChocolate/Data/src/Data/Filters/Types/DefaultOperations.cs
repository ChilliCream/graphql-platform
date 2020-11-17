namespace HotChocolate.Data.Filters
{
    public static class DefaultOperations
    {
        public new const int Equals = 0;
        public const int NotEquals = 1;

        public const int Contains = 2;
        public const int NotContains = 3;

        public const int In = 4;
        public const int NotIn = 5;

        public const int StartsWith = 6;
        public const int NotStartsWith = 7;

        public const int EndsWith = 8;
        public const int NotEndsWith = 9;

        public const int And = 10;
        public const int Or = 11;

        public const int GreaterThan = 16;
        public const int NotGreaterThan = 17;

        public const int GreaterThanOrEquals = 18;
        public const int NotGreaterThanOrEquals = 19;

        public const int LowerThan = 20;
        public const int NotLowerThan = 21;

        public const int LowerThanOrEquals = 22;
        public const int NotLowerThanOrEquals = 23;

        public const int Some = 24;
        public const int All = 25;
        public const int None = 26;
        public const int Any = 27;

        public const int Like = 28;

        public const int Data = 29;
    }
}
