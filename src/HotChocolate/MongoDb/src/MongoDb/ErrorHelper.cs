using System.Linq;
using HotChocolate.Data;
using HotChocolate.Data.Filters;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.MongoDb
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
    }
}
