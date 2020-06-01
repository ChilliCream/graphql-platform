using System;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Types.Filters.Properties;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Filters
{
    public static class FilterInputTypeDescriptorCustomExtensions
    {
        public static ICustomFilterFieldDescriptor Custom<T, TItem>(
            this IFilterInputTypeDescriptor<T> descriptor,
            Expression<Func<T, TItem>> property)
        {
            if (property.ExtractMember() is PropertyInfo p)
            {
                return descriptor.AddFilter(p,
                    ctx => new CustomFilterFieldDescriptor(
                        ctx, p, ctx.GetFilterConvention()));
            }

            throw new ArgumentException(
                FilterResources.FilterInputTypeDescriptor_OnlyProperties,
                nameof(property));
        }

        public static ICustomFilterFieldDescriptor Custom(
            this IFilterInputTypeDescriptor descriptor,
            NameString name)
        {
            if (name == default!)
            {
                throw new ArgumentNullException(nameof(name));
            }

            return descriptor.AddFilter(name,
                ctx => CustomFilterFieldDescriptor.New(
                    ctx, ctx.GetFilterConvention(), name));
        }
    }
}
