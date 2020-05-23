using System;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Types.Filters.Properties;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Filters
{
    public static class FilterInputTypeDescriptorComparableExtensions
    {
        public static IComparableFilterFieldDescriptor Filter<T>(
            this IFilterInputTypeDescriptor<T> descriptor,
            Expression<Func<T, IComparable>> property)
        {
            if (property.ExtractMember() is PropertyInfo p)
            {
                return descriptor.AddFilter(p,
                    ctx => new ComparableFilterFieldDescriptor(
                        ctx, p, ctx.GetFilterConvention()));
            }

            throw new ArgumentException(
                FilterResources.FilterInputTypeDescriptor_OnlyProperties,
                nameof(property));
        }

        public static IComparableFilterFieldDescriptor Comparable(
            this IFilterInputTypeDescriptor descriptor,
            NameString name)
        {
            if (name == default!)
            {
                throw new ArgumentNullException(nameof(name));
            }

            return descriptor.AddFilter(name,
                ctx => ComparableFilterFieldDescriptor.New(
                    ctx, ctx.GetFilterConvention(), name));
        }
    }
}
