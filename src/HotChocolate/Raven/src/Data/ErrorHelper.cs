using HotChocolate.Data.Sorting;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Data.Raven;

internal static class ErrorHelper
{
    public static IError CreateNonNullError<T>(
        ISortField field,
        IValueNode value,
        ISortVisitorContext<T> context)
    {
        var sortType = context.Types.OfType<ISortInputType>().First();

        return ErrorBuilder.New()
            .SetMessage(
                RavenDataResources.ErrorHelper_CreateNonNullError,
                context.Fields.Peek().Name,
                sortType.Print())
            .AddLocation(value)
            .SetCode(ErrorCodes.Data.NonNullError)
            .SetExtension("expectedType", new NonNullType(field.Type).Print())
            .SetExtension("sortType", sortType.Print())
            .Build();
    }
}
