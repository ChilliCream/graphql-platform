using System;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Types.Filters.Properties;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Filters
{
    public static class FilterInputTypeDescriptorObjectExtensions
    {
        /// <summary>
        /// Define a object filter for the selected property.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TObject"></typeparam>
        /// <param name="descriptor">The descriptor to extend</param>
        /// <param name="property">The property for which a filter shall be applied.</param>
        /// <returns></returns>
        public static IObjectFilterFieldDescriptor<TObject> Object<T, TObject>(
            this IFilterInputTypeDescriptor<T> descriptor,
            Expression<Func<T, TObject>> property) where TObject : class
        {
            if (property.ExtractMember() is PropertyInfo p)
            {
                return descriptor.AddFilter(p,
                    ctx => new ObjectFilterFieldDescriptor<TObject>(
                        ctx, p, ctx.GetFilterConvention()));
            }

            throw new ArgumentException(
                FilterResources.FilterInputTypeDescriptor_OnlyProperties,
                nameof(property));
        }

        public static IObjectFilterFieldDescriptor Object(
            this IFilterInputTypeDescriptor descriptor,
            NameString name)
        {
            if (name == default!)
            {
                throw new ArgumentNullException(nameof(name));
            }

            return descriptor.AddFilter(name,
                ctx => ObjectFilterFieldDescriptor.New(
                    ctx, ctx.GetFilterConvention(), name));
        }
    }
}
