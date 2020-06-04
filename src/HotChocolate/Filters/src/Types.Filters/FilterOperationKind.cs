namespace HotChocolate.Types.Filters
{
    public static class FilterOperationKind
    {
        public new const int Equals = 1;
        public const int NotEquals = 2;

        public const int Contains = 3;
        public const int NotContains = 4;

        public const int In = 5;
        public const int NotIn = 6;

        public const int StartsWith = 7;
        public const int NotStartsWith = 8;

        public const int EndsWith = 9;
        public const int NotEndsWith = 10;

        public const int GreaterThan = 11;
        public const int NotGreaterThan = 12;

        public const int GreaterThanOrEquals = 13;
        public const int NotGreaterThanOrEquals = 14;

        public const int LowerThan = 15;
        public const int NotLowerThan = 16;

        public const int LowerThanOrEquals = 17;
        public const int NotLowerThanOrEquals = 18;

        public const int Object = 19;

        public const int ArraySome = 20;

        public const int ArrayNone = 21;

        public const int ArrayAll = 22;

        public const int ArrayAny = 23;

        public const int Custom = 24;

        public const int Skip = 25;
    }
}
