using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Types.Filters.Properties;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Filters
{
    public static class FilterInputTypeDescriptorArrayExtensions
    {
        private static IArrayFilterFieldDescriptor<TObject> ListFilter<T, TObject, TListType>(
            this IFilterInputTypeDescriptor<T> descriptor,
            Expression<Func<T, TListType>> property)
        {
            if (property.ExtractMember() is PropertyInfo p)
            {
                return descriptor.AddFilter(
                    p,
                    ctx => new ArrayFilterFieldDescriptor<TObject>(
                        ctx, p, ctx.GetFilterConvention()));
            }

            throw new ArgumentException(
                FilterResources.FilterInputTypeDescriptor_OnlyProperties,
                nameof(property));
        }

        /// <summary>
        /// Define a filter for a IEnumerable of string
        /// </summary>
        /// <typeparam name="T">Type of the struct</typeparam> 
        /// <param name="descriptor">The descriptor to extend</param>
        /// <param name="property">The property of the type</param>
        /// <returns></returns>
        public static IArrayFilterFieldDescriptor<TObject> List<T, TObject>(
            this IFilterInputTypeDescriptor<T> descriptor,
            Expression<Func<T, IEnumerable<TObject>>> property)
            where TObject : class =>
                descriptor.ListFilter<T, TObject, IEnumerable<TObject>>(property);

        /// <summary>
        /// Define a filter for a IEnumerable of type string
        /// </summary> 
        /// <typeparam name="T">Type of the struct</typeparam> 
        /// <param name="descriptor">The descriptor to extend</param>
        /// <param name="property">The property of the type</param>
        /// <returns></returns>
        public static IArrayFilterFieldDescriptor<ISingleFilter<string>> List<T>(
            this IFilterInputTypeDescriptor<T> descriptor,
            Expression<Func<T, IEnumerable<string>>> property) =>
                descriptor.ListFilter<T, ISingleFilter<string>, IEnumerable<string>>(property);

        /// <summary>
        ///  Define a filter for a list of bools
        /// </summary>
        /// <typeparam name="T">Type of the struct</typeparam> 
        /// <param name="descriptor">The descriptor to extend</param>
        /// <param name="property">The property of the type</param>
        /// <returns></returns>
        public static IArrayFilterFieldDescriptor<ISingleFilter<bool>> List<T>(
            this IFilterInputTypeDescriptor<T> descriptor,
            Expression<Func<T, IEnumerable<bool>>> property) =>
                descriptor.ListFilter<T, ISingleFilter<bool>, IEnumerable<bool>>(property);

        /// <summary>
        ///  Define a filter for a list of struct
        /// </summary>
        /// <typeparam name="T">Type of the struct</typeparam>
        /// <typeparam name="TStruct">Helper for propert generic capturing</typeparam>
        /// <param name="descriptor">The descriptor to extend</param>
        /// <param name="property">The property of the type</param>
        /// <param name="_">Ignore this</param>
        /// <returns></returns>
        public static IArrayFilterFieldDescriptor<ISingleFilter<TStruct>> List<T, TStruct>(
            this IFilterInputTypeDescriptor<T> descriptor,
            Expression<Func<T, IEnumerable<TStruct>>> property,
            RequireStruct<TStruct>? _ = null)
            where TStruct : struct =>
                descriptor.ListFilter<T, ISingleFilter<TStruct>, IEnumerable<TStruct>>(property);

        /// <summary>
        ///  Define a filter for a struct
        /// </summary>
        /// <typeparam name="T">Type of the struct</typeparam>
        /// <typeparam name="TStruct">Helper for propert generic capturing</typeparam>
        /// <param name="descriptor">The descriptor to extend</param>
        /// <param name="property">The property of the type</param>
        /// <param name="_">Ignore this</param>
        /// <returns></returns>
        public static IArrayFilterFieldDescriptor<ISingleFilter<TStruct>> List<T, TStruct>(
            this IFilterInputTypeDescriptor<T> descriptor,
            Expression<Func<T, IEnumerable<TStruct?>>> property,
            RequireStruct<TStruct>? _ = null)
            where TStruct : struct =>
                descriptor.ListFilter<T, ISingleFilter<TStruct>, IEnumerable<TStruct?>>(property);

        public class RequireStruct<TStruct> where TStruct : struct { }
    }
}
