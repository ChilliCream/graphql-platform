using System.Globalization;
using System.Reflection;
using HotChocolate.Data.Sorting;
using HotChocolate.Types;

namespace HotChocolate.Data.Marten;

internal static class ThrowHelper
{
    public static InvalidOperationException Sorting_InvalidState_ParentIsNoFieldSelector(
        ISortField field) =>
        new(string.Format(
            CultureInfo.CurrentCulture,
            MartenDataResources.Sorting_InvalidState_ParentIsNoFieldSelector,
            field.DeclaringType.Print(),
            field.Name,
            field.Type.Print()));

    public static InvalidOperationException QueryableSorting_ExpressionParameterInvalid(
        Type type,
        ISortField field) =>
        new(string.Format(
            CultureInfo.CurrentCulture,
            MartenDataResources.QueryableSorting_ExpressionParameterInvalid,
            type.FullName,
            field.DeclaringType.Print(),
            field.Name,
            field.Type.Print()));

    public static InvalidOperationException QueryableSorting_MemberInvalid(
        MemberInfo memberInfo,
        ISortField field) =>
        new(string.Format(
            CultureInfo.CurrentCulture,
            MartenDataResources.QueryableSorting_MemberInvalid,
            memberInfo.GetType(),
            field.DeclaringType.Print(),
            field.Name,
            field.Type.Print()));

    public static InvalidOperationException QueryableSorting_NoMemberDeclared(ISortField field) =>
        new(string.Format(
            CultureInfo.CurrentCulture,
            MartenDataResources.QueryableSorting_NoMemberDeclared,
            field.DeclaringType.Print(),
            field.Name,
            field.Type.Print()));
}
