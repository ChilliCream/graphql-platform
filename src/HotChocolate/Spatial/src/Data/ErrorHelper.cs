using System;
using System.Linq;
using HotChocolate.Data.Filters;
using HotChocolate.Data.Sorting;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Types;

namespace HotChocolate.Data
{
    internal static class ErrorHelper
    {
        public static IError CreateNonNullError<T>(
            IFilterField field,
            IValueNode value,
            IFilterVisitorContext<T> context)
        {
            IFilterInputType filterType = context.Types.OfType<IFilterInputType>().First();

            return ErrorBuilder.New()
                .SetMessage(
                    DataResources.ErrorHelper_CreateNonNullError,
                    context.Operations.Peek().Name,
                    filterType.Visualize())
                .AddLocation(value)
                .SetExtension("expectedType", new NonNullType(field.Type).Visualize())
                .SetExtension("filterType", filterType.Visualize())
                .Build();
        }

        public static IError CouldNotCreateFilterForOperation<T>(
            IFilterField field,
            IValueNode value,
            IFilterVisitorContext<T> context)
        {
            IFilterInputType filterType = context.Types.OfType<IFilterInputType>().First();

            return ErrorBuilder.New()
                .SetMessage(
                    DataResources.CouldNotCreateFilterForOperation,
                    context.Operations.Peek().Name,
                    filterType.Visualize())
                .AddLocation(value)
                .SetExtension("expectedType", new NonNullType(field.Type).Visualize())
                .SetExtension("filterType", filterType.Visualize())
                .Build();
        }

        public static IError CreateNonNullError<T>(
            ISortField field,
            IValueNode value,
            ISortVisitorContext<T> context)
        {
            ISortInputType sortType = context.Types.OfType<ISortInputType>().First();

            return ErrorBuilder.New()
                .SetMessage(
                    DataResources.ErrorHelper_CreateNonNullError,
                    context.Fields.Peek().Name,
                    sortType.Visualize())
                .AddLocation(value)
                .SetExtension("expectedType", new NonNullType(field.Type).Visualize())
                .SetExtension("sortType", sortType.Visualize())
                .Build();
        }
    }
}
