using System;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Types.Filters.Properties;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Filters
{
    public static class FilterInputTypeDescriptorSkipExtensions
    {
        public static ICustomFilterFieldDescriptor Skip<T, TItem>(
            this IFilterInputTypeDescriptor<T> descriptor,
            Expression<Func<T, TItem>> property) =>
            descriptor.Custom(property).Kind(FilterKind.Skip);

        public static ICustomFilterFieldDescriptor Skip(
            this IFilterInputTypeDescriptor descriptor,
            NameString name) =>
            descriptor.Custom(name).Kind(FilterKind.Skip);
    }
}
