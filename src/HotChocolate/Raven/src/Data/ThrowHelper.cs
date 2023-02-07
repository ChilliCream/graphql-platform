using System.Globalization;
using System.Reflection;
using HotChocolate.Data.Filters;
using HotChocolate.Data.Sorting;
using HotChocolate.Language;
using HotChocolate.Language.Utilities;
using HotChocolate.Types;

namespace HotChocolate.Data.Raven;

internal static class ThrowHelper
{
    public static InvalidOperationException Sorting_InvalidState_ParentIsNoFieldSelector(
        ISortField field) =>
        new(string.Format(
            CultureInfo.CurrentCulture,
            RavenDataResources.Sorting_InvalidState_ParentIsNoFieldSelector,
            field.DeclaringType.Print(),
            field.Name,
            field.Type.Print()));

    public static InvalidOperationException QueryableSorting_ExpressionParameterInvalid(
        Type type,
        ISortField field) =>
        new(string.Format(
            CultureInfo.CurrentCulture,
            RavenDataResources.QueryableSorting_ExpressionParameterInvalid,
            type.FullName,
            field.DeclaringType.Print(),
            field.Name,
            field.Type.Print()));

    public static InvalidOperationException QueryableSorting_MemberInvalid(
        MemberInfo memberInfo,
        ISortField field) =>
        new(string.Format(
            CultureInfo.CurrentCulture,
            RavenDataResources.QueryableSorting_MemberInvalid,
            memberInfo.GetType(),
            field.DeclaringType.Print(),
            field.Name,
            field.Type.Print()));

    public static InvalidOperationException QueryableSorting_NoMemberDeclared(ISortField field) =>
        new(string.Format(
            CultureInfo.CurrentCulture,
            RavenDataResources.QueryableSorting_NoMemberDeclared,
            field.DeclaringType.Print(),
            field.Name,
            field.Type.Print()));

    public static InvalidOperationException Filtering_CouldNotParseValue(
        IFilterFieldHandler handler,
        IValueNode valueNode,
        IType expectedType,
        IFilterField field) =>
        new(string.Format(
            CultureInfo.CurrentCulture,
            RavenDataResources.Filtering_CouldNotParseValue,
            handler.GetType().Name,
            valueNode.Print(),
            expectedType.Print(),
            field.Name,
            field.Type.Print()));

    public static GraphQLException PagingTypeNotSupported(Type type)
    {
        return new GraphQLException(
            ErrorBuilder.New()
                .SetMessage(
                    RavenDataResources.Paging_SourceIsNotSupported,
                    type.FullName ?? type.Name)
                .SetCode(ErrorCodes.Data.NoPaginationProviderFound)
                .Build());
    }
}
