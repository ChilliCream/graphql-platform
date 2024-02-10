using System.Globalization;
using HotChocolate.Data.Filters;
using HotChocolate.Language;
using HotChocolate.Language.Utilities;
using HotChocolate.Types;

namespace HotChocolate.Data.Marten;

internal static class ThrowHelper
{
    public static InvalidOperationException Filtering_CouldNotParseValue(
        IFilterFieldHandler handler,
        IValueNode valueNode,
        IType expectedType,
        IFilterField field) =>
        new(string.Format(
            CultureInfo.CurrentCulture,
            MartenDataResources.Filtering_CouldNotParseValue,
            handler.GetType().Name,
            valueNode.Print(),
            expectedType.Print(),
            field.Name,
            field.Type.Print()));
}
