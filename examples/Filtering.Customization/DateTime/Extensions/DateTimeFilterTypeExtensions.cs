
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Filters;
using HotChocolate.Types.Filters.Conventions;
using HotChocolate.Types.Filters.Expressions;
using HotChocolate.Utilities;

namespace Filtering.Customization
{
    public static class DateTimeFilterTypeExtensions
    {
        public static IDateTimeFilterFieldDescriptor Filter<T>(
            this IFilterInputTypeDescriptor<T> descriptor,
            Expression<Func<T, DateTime>> property)
        {
            if (property.ExtractMember() is PropertyInfo p)
            {
                return descriptor.AddFilter(
                    p,
                    ctx => new DateTimeFilterFieldDescriptor(ctx, p, ctx.GetFilterConvention()));
            }

            throw new ArgumentException("Only properties allowed", nameof(property));
        }
    }
}