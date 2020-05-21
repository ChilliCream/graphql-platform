using System;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Types.Filters.Properties;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Filters
{
    public static class FilterInputTypeDescriptorBooleanExtensions
    {
        /// <summary>
        /// Define a boolean filter for the selected property.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="descriptor">The descriptor to extend</param>
        /// <param name="property">The property for which a filter shall be applied.</param>
        /// <returns></returns>
        public static IBooleanFilterFieldDescriptor Filter<T>(
            this IFilterInputTypeDescriptor<T> descriptor,
            Expression<Func<T, bool>> property)
        {
            if (property.ExtractMember() is PropertyInfo p)
            {
                return descriptor.AddFilter(p,
                    ctx => new BooleanFilterFieldDescriptor(
                        ctx, p, ctx.GetFilterConvention()));
            }

            throw new ArgumentException(
                FilterResources.FilterInputTypeDescriptor_OnlyProperties,
                nameof(property));
        }

        /// <summary>
        /// Define a boolean filter for the selected property.
        /// </summary>
        /// <param name="descriptor">The descriptor to extend</param>
        /// <param name="name">The name of the named filter</param>
        /// <returns></returns>
        public static IBooleanFilterFieldDescriptor Boolean(
            this IFilterInputTypeDescriptor descriptor,
            NameString name)
        {
            if (name == default!)
            {
                throw new ArgumentNullException(nameof(name));
            }

            return descriptor.AddFilter(name,
                ctx => BooleanFilterFieldDescriptor.New(
                    ctx, ctx.GetFilterConvention(), name));
        }
    }
}
