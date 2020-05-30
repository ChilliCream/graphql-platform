namespace HotChocolate.Types.Filters
{
    public static class FilterOperationKind
    {
        public new const string Equals = "Equals";
        public const string NotEquals = "NotEquals";

        public const string Contains = "Contains";
        public const string NotContains = "NotContains";

        public const string In = "In";
        public const string NotIn = "NotIn";

        public const string StartsWith = "StartsWith";
        public const string NotStartsWith = "NotStartsWith";

        public const string EndsWith = "EndsWith";
        public const string NotEndsWith = "NotEndsWith";

        public const string GreaterThan = "GreaterThan";
        public const string NotGreaterThan = "NotGreaterThan";

        public const string GreaterThanOrEquals = "GreaterThanOrEquals";
        public const string NotGreaterThanOrEquals = "NotGreaterThanOrEquals";

        public const string LowerThan = "LowerThan";
        public const string NotLowerThan = "NotLowerThan";

        public const string LowerThanOrEquals = "LowerThanOrEquals";
        public const string NotLowerThanOrEquals = "NotLowerThanOrEquals";

        public const string Object = "Object";

        public const string ArraySome = "ArraySome";

        public const string ArrayNone = "ArrayNone";

        public const string ArrayAll = "ArrayAll";

        public const string ArrayAny = "ArrayAny";
    }
}
