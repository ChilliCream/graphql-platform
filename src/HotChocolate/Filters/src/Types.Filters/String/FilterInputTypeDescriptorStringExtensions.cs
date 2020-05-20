using System;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Types.Filters.Properties;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Filters
{
    public static class FilterInputTypeDescriptorStringExtensions
    {
        /// <summary>
        /// Define a string filter for the selected property.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="descriptor">The descriptor to extend</param>
        /// <param name="property">The property for which a filter shall be applied.</param>
        /// <returns></returns>
        public static IStringFilterFieldDescriptor Filter<T>(
            this IFilterInputTypeDescriptor<T> descriptor,
            Expression<Func<T, string>> property)
        {
            if (property.ExtractMember() is PropertyInfo p)
            {
                return descriptor.AddFilter(p,
                    ctx => new StringFilterFieldDescriptor(
                        ctx, p, ctx.GetFilterConvention()));
            }

            throw new ArgumentException(
                FilterResources.FilterInputTypeDescriptor_OnlyProperties,
                nameof(property));
        }
    }
}
