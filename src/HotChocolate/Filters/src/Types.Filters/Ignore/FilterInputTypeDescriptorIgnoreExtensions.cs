using System;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Types.Filters.Properties;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Filters
{
    public static class FilterInputTypeDescriptorIgnoreExtensions
    {
        /// <summary>
        /// ignores filter for the selected property.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="descriptor">The descriptor to extend</param>
        /// <param name="property">The property for which a filter shall be applied.</param>
        /// <returns></returns>
        public static IFilterInputTypeDescriptor<T> Ignore<T>(
            this IFilterInputTypeDescriptor<T> descriptor,
            Expression<Func<T, object>> property)
        {
            if (property.ExtractMember() is PropertyInfo p)
            {
                descriptor.AddFilter(p,
                    ctx => new IgnoredFilterFieldDescriptor(
                        ctx, p, ctx.GetFilterConvention()));
                return descriptor;
            }

            throw new ArgumentException(
                FilterResources.FilterInputTypeDescriptor_OnlyProperties,
                nameof(property));
        }
    }
}
