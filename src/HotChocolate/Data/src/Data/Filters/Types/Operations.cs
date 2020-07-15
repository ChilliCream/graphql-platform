using System;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Data.Filters
{
    public static class Operations
    {
        public const int Equals = 0;
        public const int NotEquals = 1;

        public const int Contains = 2;
        public const int NotContains = 3;

        public const int In = 4;
        public const int NotIn = 5;

        public const int StartsWith = 6;
        public const int NotStartsWith = 7;

        public const int EndsWith = 8;
        public const int NotEndsWith = 9;

        public const int GreaterThan = 16;
        public const int NotGreaterThan = 17;

        public const int GreaterThanOrEquals = 18;
        public const int NotGreaterThanOrEquals = 19;

        public const int LowerThan = 20;
        public const int NotLowerThan = 21;

        public const int LowerThanOrEquals = 22;
        public const int NotLowerThanOrEquals = 23;
    }
}