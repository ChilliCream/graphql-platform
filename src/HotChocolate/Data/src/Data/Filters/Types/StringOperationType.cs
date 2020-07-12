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

    public class StringOperationType : FilterInputType
    {
        protected override void Configure(IFilterInputTypeDescriptor descriptor)
        {
            descriptor.Operation(Operations.Equals).Type<StringType>();
            descriptor.Operation(Operations.NotEquals).Type<StringType>();
            descriptor.Operation(Operations.Contains).Type<StringType>();
            descriptor.Operation(Operations.NotContains).Type<StringType>();
            descriptor.Operation(Operations.In).Type<StringType>();
            descriptor.Operation(Operations.NotIn).Type<StringType>();
            descriptor.Operation(Operations.StartsWith).Type<StringType>();
            descriptor.Operation(Operations.NotStartsWith).Type<StringType>();
            descriptor.Operation(Operations.EndsWith).Type<StringType>();
            descriptor.Operation(Operations.NotEndsWith).Type<StringType>();
            descriptor.Operation(Operations.GreaterThan).Type<StringType>();
            descriptor.Operation(Operations.NotGreaterThan).Type<StringType>();
            descriptor.Operation(Operations.GreaterThanOrEquals).Type<StringType>();
            descriptor.Operation(Operations.NotGreaterThanOrEquals).Type<StringType>();
            descriptor.Operation(Operations.LowerThan).Type<StringType>();
            descriptor.Operation(Operations.NotLowerThan).Type<StringType>();
            descriptor.Operation(Operations.LowerThanOrEquals).Type<StringType>();
            descriptor.Operation(Operations.NotLowerThanOrEquals).Type<StringType>();
        }
    }

    public class ComparableOperationType<T> : FilterInputType
        where T : IComparable
    {
        protected override void Configure(IFilterInputTypeDescriptor descriptor)
        {
            descriptor.Operation(Operations.Equals).Type(typeof(T));
            descriptor.Operation(Operations.NotEquals).Type(typeof(T));
            descriptor.Operation(Operations.Contains).Type(typeof(T));
            descriptor.Operation(Operations.NotContains).Type(typeof(T));
            descriptor.Operation(Operations.In).Type(typeof(T));
            descriptor.Operation(Operations.NotIn).Type(typeof(T));
            descriptor.Operation(Operations.StartsWith).Type(typeof(T));
            descriptor.Operation(Operations.NotStartsWith).Type(typeof(T));
            descriptor.Operation(Operations.EndsWith).Type(typeof(T));
            descriptor.Operation(Operations.NotEndsWith).Type(typeof(T));
            descriptor.Operation(Operations.GreaterThan).Type(typeof(T));
            descriptor.Operation(Operations.NotGreaterThan).Type(typeof(T));
            descriptor.Operation(Operations.GreaterThanOrEquals).Type(typeof(T));
            descriptor.Operation(Operations.NotGreaterThanOrEquals).Type(typeof(T));
            descriptor.Operation(Operations.LowerThan).Type(typeof(T));
            descriptor.Operation(Operations.NotLowerThan).Type(typeof(T));
            descriptor.Operation(Operations.LowerThanOrEquals).Type(typeof(T));
            descriptor.Operation(Operations.NotLowerThanOrEquals).Type(typeof(T));
        }

    }
    public class BooleanOperationType : FilterInputType
    {
        protected override void Configure(IFilterInputTypeDescriptor descriptor)
        {
            descriptor.Operation(Operations.Equals).Type<StringType>();
            descriptor.Operation(Operations.NotEquals).Type<StringType>();
        }
    }
}