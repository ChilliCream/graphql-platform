using System.Linq;
using HotChocolate.Data.Filters;
using HotChocolate.Language;
using HotChocolate.Types;
using static HotChocolate.Data.DataResources;

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
                    "The provided value for filter `{0}` of type {1} is invalid. " +
                    "Null values are not supported.",
                    context.Operations.Peek().Name,
                    filterType.Visualize())
                .AddLocation(value)
                .SetExtension("expectedType", new NonNullType(field.Type).Visualize())
                .SetExtension("filterType", filterType.Visualize())
                .Build();
        }

        public static ISchemaError FilterField_RuntimeType_Unknown(FilterField field) =>
            SchemaErrorBuilder.New()
                .SetMessage(
                    FilterField_FilterField_TypeUnknown,
                    field.DeclaringType.Name,
                    field.Name)
                .SetTypeSystemObject(field.DeclaringType)
                .SetExtension(nameof(field), field)
                .Build();
    }
}
