using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Utilities;

namespace HotChocolate.Data.Filters
{
    public class FilterTypeInfo
    {
        public FilterTypeInfo(
            bool isNullable,
            Type type,
            FilterTypeInfo[] isGenericNullable)
        {
            IsNullable = isNullable;
            Type = type;
            TypeArguments = isGenericNullable;
        }

        public bool IsNullable { get; }

        public Type Type { get; }

        public IReadOnlyCollection<FilterTypeInfo> TypeArguments { get; }

        internal static FilterTypeInfo From(IExtendedType typeInfo) =>
            new FilterTypeInfo(
                typeInfo.IsNullable,
                typeInfo.Type,
                typeInfo.TypeArguments.Select(From).ToArray());
    }
}
