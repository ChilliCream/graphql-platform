using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Data.Filters;
using HotChocolate.Data.Filters.Expressions;
using HotChocolate.Data.Projections;
using HotChocolate.Data.Sorting;
using HotChocolate.Language;
using HotChocolate.Language.Utilities;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Data;

internal static class ThrowHelper
{
    public static SchemaException FilterConvention_TypeOfMemberIsUnknown(MemberInfo member) =>
        new SchemaException(
            SchemaErrorBuilder.New()
                .SetMessage(
                    DataResources.FilterConvention_TypeOfMemberIsUnknown,
                    member.Name,
                    member.DeclaringType?.Name)
                .Build());

    public static SchemaException FilterConvention_TypeIsUnknown(Type type) =>
        new SchemaException(
            SchemaErrorBuilder.New()
                .SetMessage(DataResources.FilterConvention_TypeIsUnknown, type.Name)
                .Build());

    public static SchemaException FilterConvention_OperationNameNotFound(int operation) =>
        new SchemaException(
            SchemaErrorBuilder.New()
                .SetMessage(
                    DataResources.FilterConvention_OperationNameNotFound,
                    operation)
                .Build());

    public static SchemaException FilterConvention_NoProviderFound(
        Type convention,
        string? scope) =>
        new SchemaException(
            SchemaErrorBuilder.New()
                .SetMessage(
                    DataResources.FilterConvention_NoProviderFound,
                    convention.FullName ?? convention.Name)
                .SetExtension(nameof(scope), scope)
                .Build());

    public static SchemaException FilterConvention_ProviderHasToBeInitializedByConvention(
        Type provider,
        string? scope) =>
        new SchemaException(
            SchemaErrorBuilder.New()
                .SetMessage(
                    DataResources.FilterConvention_ProviderHasToBeInitializedByConvention,
                    provider.FullName ?? provider.Name,
                    scope is null ? "" : "in scope " + scope)
                .SetExtension(nameof(scope), scope)
                .Build());

    public static SchemaException FilterProvider_NoHandlersConfigured(
        IFilterProvider filterProvider) =>
        new SchemaException(
            SchemaErrorBuilder.New()
                .SetMessage(
                    DataResources.FilterProvider_NoHandlersConfigured,
                    filterProvider.GetType().FullName ?? filterProvider.GetType().Name)
                .SetExtension(nameof(filterProvider), filterProvider)
                .Build());

    public static SchemaException Filtering_FilteringWasNotFound(IResolverContext context) =>
        new SchemaException(
            SchemaErrorBuilder.New()
                .SetMessage(DataResources.Filtering_FilteringWasNotFound)
                .SetPath(context.Path)
                .SetExtension("fieldName", context.Selection.Field.Name)
                .SetExtension("typeName", context.Selection.Type.NamedType().Name)
                .Build());

    public static SchemaException Filtering_TypeMismatch(
        IResolverContext context,
        Type expectedType,
        Type resultType) =>
        new SchemaException(
            SchemaErrorBuilder.New()
                .SetMessage(
                    DataResources.Filtering_TypeMismatch,
                    expectedType.FullName ?? expectedType.Name,
                    resultType.FullName ?? resultType.Name)
                .SetPath(context.Path)
                .SetExtension("fieldName", context.Selection.Field.Name)
                .SetExtension("typeName", context.Selection.Type.NamedType().Name)
                .Build());

    public static SchemaException FilterInterceptor_NoHandlerFoundForField(
        FilterInputTypeDefinition type,
        FilterFieldDefinition field) =>
        new SchemaException(
            SchemaErrorBuilder.New()
                .SetMessage(
                    DataResources.FilterInterceptor_NoHandlerFoundForField,
                    field.Name,
                    type.Name)
                .Build());

    public static SchemaException FilterInterceptor_OperationHasNoTypeSpecified(
        FilterInputTypeDefinition type,
        FilterFieldDefinition field) =>
        new SchemaException(
            SchemaErrorBuilder.New()
                .SetMessage(
                    DataResources.FilterInterceptor_OperationHasNoTypeSpecified,
                    field.Name,
                    type.Name)
                .Build());

    public static GraphQLException FilterConvention_CouldNotConvertValue(IValueNode node) =>
        new GraphQLException(
            ErrorBuilder.New()
                .SetMessage(DataResources.FilterConvention_CouldNotConvertValue)
                .AddLocation(node)
                .Build());

    public static SchemaException FilterObjectFieldDescriptorExtensions_CannotInfer() =>
        new SchemaException(
            SchemaErrorBuilder.New()
                .SetMessage(DataResources.FilterObjectFieldDescriptorExtensions_CannotInfer)
                .SetCode(ErrorCodes.Filtering.FilterObjectType)
                .Build());

    public static SchemaException FilterDescriptorContextExtensions_NoConvention(string? scope) =>
        new SchemaException(
            SchemaErrorBuilder.New()
                .SetMessage(
                    scope is null
                        ? DataResources.FilterDescriptorContextExtensions_NoConvention_Default
                        : DataResources.FilterDescriptorContextExtensions_NoConvention,
                    scope ?? "none")
                .SetExtension(nameof(scope), scope)
                .Build());

    public static SchemaException FilterProvider_UnableToCreateFieldHandler(
        IFilterProvider filterProvider,
        Type fieldHandler) =>
        new SchemaException(
            SchemaErrorBuilder.New()
                .SetMessage(
                    DataResources.FilterProvider_UnableToCreateFieldHandler,
                    fieldHandler.FullName ?? fieldHandler.Name,
                    filterProvider.GetType().FullName ?? filterProvider.GetType().Name)
                .SetExtension(nameof(filterProvider), filterProvider)
                .SetExtension(nameof(fieldHandler), fieldHandler)
                .Build());

    public static SchemaException SortProvider_UnableToCreateFieldHandler(
        ISortProvider sortProvider,
        Type fieldHandler) =>
        new SchemaException(
            SchemaErrorBuilder.New()
                .SetMessage(
                    DataResources.SortProvider_UnableToCreateFieldHandler,
                    fieldHandler.FullName ?? fieldHandler.Name,
                    sortProvider.GetType().FullName ?? sortProvider.GetType().Name)
                .SetExtension(nameof(sortProvider), sortProvider)
                .SetExtension(nameof(fieldHandler), fieldHandler)
                .Build());

    public static SchemaException SortProvider_UnableToCreateOperationHandler(
        ISortProvider sortProvider,
        Type operationHandler) =>
        new SchemaException(
            SchemaErrorBuilder.New()
                .SetMessage(
                    DataResources.SortProvider_UnableToCreateOperationHandler,
                    operationHandler.FullName ?? operationHandler.Name,
                    sortProvider.GetType().FullName ?? sortProvider.GetType().Name)
                .SetExtension(nameof(sortProvider), sortProvider)
                .SetExtension(nameof(operationHandler), operationHandler)
                .Build());

    public static SchemaException SortProvider_NoFieldHandlersConfigured(
        ISortProvider filterProvider) =>
        new SchemaException(
            SchemaErrorBuilder.New()
                .SetMessage(
                    DataResources.SortProvider_NoFieldHandlersConfigured,
                    filterProvider.GetType().FullName ?? filterProvider.GetType().Name)
                .SetExtension(nameof(filterProvider), filterProvider)
                .Build());

    public static SchemaException SortProvider_NoOperationHandlersConfigured(
        ISortProvider sortProvider) =>
        new SchemaException(
            SchemaErrorBuilder.New()
                .SetMessage(
                    DataResources.SortProvider_NoOperationHandlersConfigured,
                    sortProvider.GetType().FullName ?? sortProvider.GetType().Name)
                .SetExtension(nameof(sortProvider), sortProvider)
                .Build());

    public static SchemaException SortDescriptorContextExtensions_NoConvention(string? scope) =>
        new SchemaException(
            SchemaErrorBuilder.New()
                .SetMessage(
                    scope is null
                        ? DataResources.SortDescriptorContextExtensions_NoConvention_Default
                        : DataResources.SortDescriptorContextExtensions_NoConvention,
                    scope ?? "none")
                .SetExtension(nameof(scope), scope)
                .Build());

    public static SchemaException SortInterceptor_NoFieldHandlerFoundForField(
        SortInputTypeDefinition type,
        SortFieldDefinition field) =>
        new SchemaException(
            SchemaErrorBuilder.New()
                .SetMessage(
                        DataResources.SortInterceptor_NoFieldHandlerFoundForField,
                    field.Name,
                    type.Name)
                .Build());

    public static SchemaException SortInterceptor_NoOperationHandlerFoundForValue(
        EnumTypeDefinition type,
        SortEnumValueDefinition value) =>
        new SchemaException(
            SchemaErrorBuilder.New()
                .SetMessage(
                    DataResources.SortInterceptor_NoOperationHandlerFoundForValue,
                    value.Name,
                    type.Name)
                .Build());

    public static SchemaException SortConvention_NoProviderFound(
        Type convention,
        string? scope) =>
        new SchemaException(
            SchemaErrorBuilder.New()
                .SetMessage(
                    DataResources.SortConvention_NoProviderFound,
                    convention.FullName ?? convention.Name)
                .SetExtension(nameof(scope), scope)
                .Build());

    public static SchemaException SortConvention_TypeOfMemberIsUnknown(MemberInfo member) =>
        new SchemaException(
            SchemaErrorBuilder.New()
                .SetMessage(
                    DataResources.SortConvention_TypeOfMemberIsUnknown,
                    member.Name,
                    member.DeclaringType?.Name)
                .Build());

    public static SchemaException SortConvention_OperationNameNotFound(int operation) =>
        new SchemaException(
            SchemaErrorBuilder.New()
                .SetMessage(
                    DataResources.SortConvention_OperationNameNotFound,
                    operation)
                .Build());

    public static SchemaException SortObjectFieldDescriptorExtensions_CannotInfer() =>
        new SchemaException(
            SchemaErrorBuilder.New()
                .SetMessage(DataResources.SortObjectFieldDescriptorExtensions_CannotInfer)
                .SetCode(ErrorCodes.Filtering.FilterObjectType)
                .Build());

    public static SchemaException SortConvention_OperationIsNotNamed(
        ISortConvention sortConvention,
        SortOperation sortOperation) =>
        new SchemaException(
            SchemaErrorBuilder.New()
                .SetMessage(
                    DataResources.SortProvider_UnableToCreateFieldHandler,
                    sortOperation.Id,
                    sortConvention.GetType().FullName ?? sortConvention.GetType().Name,
                    sortConvention.Scope ?? "Default")
                .SetExtension(nameof(sortConvention), sortConvention)
                .SetExtension(nameof(sortOperation), sortOperation)
                .Build());

    public static SchemaException Sorting_TypeOfInvalidFormat(IType type) =>
        new SchemaException(
            SchemaErrorBuilder.New()
                .SetMessage(
                    DataResources.Sorting_TypeOfInvalidFormat,
                    type.Print())
                .Build());

    public static SchemaException Sorting_SortingWasNotFound(IResolverContext context) =>
        new SchemaException(
            SchemaErrorBuilder.New()
                .SetMessage(DataResources.Sorting_SortingWasNotFound)
                .SetPath(context.Path)
                .SetExtension("fieldName", context.Selection.Field.Name)
                .SetExtension("typeName", context.Selection.Type.NamedType().Name)
                .Build());

    public static SchemaException Sorting_TypeMismatch(
        IResolverContext context,
        Type expectedType,
        Type resultType) =>
        new SchemaException(
            SchemaErrorBuilder.New()
                .SetMessage(
                    DataResources.Sorting_TypeMismatch,
                    expectedType.FullName ?? expectedType.Name,
                    resultType.FullName ?? resultType.Name)
                .SetPath(context.Path)
                .SetExtension("fieldName", context.Selection.Field.Name)
                .SetExtension("typeName", context.Selection.Type.NamedType().Name)
                .Build());

    public static SchemaException ProjectionProvider_NoHandlersConfigured(
        IProjectionProvider projectionConvention) =>
        new SchemaException(
            SchemaErrorBuilder.New()
                .SetMessage(
                    DataResources.ProjectionProvider_NoHandlersConfigured,
                    projectionConvention.GetType().FullName ??
                    projectionConvention.GetType().Name)
                .SetExtension(nameof(projectionConvention), projectionConvention)
                .Build());

    public static SchemaException ProjectionConvention_NoProviderFound(
        Type convention,
        string? scope) =>
        new SchemaException(
            SchemaErrorBuilder.New()
                .SetMessage(
                    DataResources.ProjectionConvention_NoProviderFound,
                    convention.FullName ?? convention.Name)
                .SetExtension(nameof(scope), scope)
                .Build());

    public static SchemaException ProjectionConvention_CouldNotProject() =>
        new SchemaException(
            SchemaErrorBuilder.New()
                .SetMessage(DataResources.ProjectionConvention_CouldNotProject)
                .Build());

    public static SchemaException ProjectionConvention_NodeFieldWasInInvalidState() =>
        new SchemaException(
            SchemaErrorBuilder.New()
                .SetMessage(DataResources.ProjectionConvention_NodeFieldWasInInvalidState)
                .Build());

    public static SchemaException Projection_ProjectionWasNotFound(IResolverContext context) =>
        new SchemaException(
            SchemaErrorBuilder.New()
                .SetMessage(DataResources.Projection_ProjectionWasNotFound)
                .SetPath(context.Path)
                .SetExtension("fieldName", context.Selection.Field.Name)
                .SetExtension("typeName", context.Selection.Type.NamedType().Name)
                .Build());

    public static SchemaException Projection_TypeMismatch(
        IResolverContext context,
        Type expectedType,
        Type resultType) =>
        new SchemaException(
            SchemaErrorBuilder.New()
                .SetMessage(
                    DataResources.Projection_TypeMismatch,
                    expectedType.FullName ?? expectedType.Name,
                    resultType.FullName ?? resultType.Name)
                .SetPath(context.Path)
                .SetExtension("fieldName", context.Selection.Field.Name)
                .SetExtension("typeName", context.Selection.Type.NamedType().Name)
                .Build());

    public static InvalidOperationException PagingProjectionOptimizer_NotAPagingField(
        IType actualType,
        IObjectField fieldName) =>
        new(string.Format(
            CultureInfo.InvariantCulture,
            DataResources.PagingProjectionOptimizer_NotAPagingField,
            actualType.Print(),
            fieldName.Name,
            fieldName.Type.Print()));

    public static InvalidOperationException Filtering_CouldNotParseValue(
        IFilterFieldHandler handler,
        IValueNode valueNode,
        IType expectedType,
        IFilterField field) =>
        new(string.Format(
            CultureInfo.CurrentCulture,
            DataResources.Filtering_CouldNotParseValue,
            handler.GetType().Name,
            valueNode.Print(),
            expectedType.Print(),
            field.Name,
            field.Type.Print()));

    public static InvalidOperationException QueryableFiltering_MemberInvalid(
        MemberInfo memberInfo,
        IFilterField field) =>
        new(string.Format(
            CultureInfo.CurrentCulture,
            DataResources.QueryableFiltering_MemberInvalid,
            memberInfo.GetType(),
            field.DeclaringType.Print(),
            field.Name,
            field.Type.Print()));

    public static InvalidOperationException QueryableFiltering_ExpressionParameterInvalid(
        Type type,
        IFilterField field) =>
        new(string.Format(
            CultureInfo.CurrentCulture,
            DataResources.QueryableFiltering_ExpressionParameterInvalid,
            type.FullName,
            field.DeclaringType.Print(),
            field.Name,
            field.Type.Print()));

    public static SchemaException QueryableFilterProvider_ExpressionParameterInvalid(
        ITypeSystemObject type,
        IFilterInputTypeDefinition typeDefinition,
        IFilterFieldDefinition field) =>
        new(SchemaErrorBuilder
                .New()
                .SetMessage(
                    DataResources.QueryableFilterProvider_ExpressionParameterInvalid,
                    typeDefinition.EntityType?.FullName,
                    field.Name)
                .SetTypeSystemObject(type)
                .Build());

    public static InvalidOperationException QueryableFiltering_NoMemberDeclared(IFilterField field)
        =>
            new(string.Format(
                CultureInfo.CurrentCulture,
                DataResources.QueryableFiltering_NoMemberDeclared,
                field.DeclaringType.Print(),
                field.Name,
                field.Type.Print()));

    public static InvalidOperationException Filtering_QueryableCombinator_QueueEmpty(
        QueryableCombinator combinator) =>
        new(string.Format(
            CultureInfo.CurrentCulture,
            DataResources.Filtering_QueryableCombinator_QueueEmpty,
            combinator.GetType()));

    public static InvalidOperationException Filtering_QueryableCombinator_InvalidCombinator(
        QueryableCombinator combinator,
        FilterCombinator operation) =>
        new(string.Format(
            CultureInfo.CurrentCulture,
            DataResources.Filtering_QueryableCombinator_InvalidCombinator,
            combinator.GetType(),
            operation.ToString()));

    public static InvalidOperationException ProjectionVisitor_MemberInvalid(MemberInfo memberInfo)
        =>
            new(string.Format(
                CultureInfo.CurrentCulture,
                DataResources.ProjectionVisitor_MemberInvalid,
                memberInfo.GetType()));

    public static InvalidOperationException ProjectionVisitor_NoMemberFound() =>
        new(string.Format(
            CultureInfo.CurrentCulture,
            DataResources.ProjectionVisitor_NoMemberFound));

    public static InvalidOperationException ProjectionVisitor_InvalidState_NoParentScope() =>
        new(string.Format(
            CultureInfo.CurrentCulture,
            DataResources.ProjectionVisitor_InvalidState_NoParentScope));

    public static InvalidOperationException ProjectionVisitor_NoConstructorFoundForSet(
        Expression expression,
        Type setType) =>
        new(string.Format(
            CultureInfo.CurrentCulture,
            DataResources.ProjectionVisitor_NoConstructorFoundForSet,
            expression.ToString(),
            setType.FullName));

    public static InvalidOperationException Sorting_InvalidState_ParentIsNoFieldSelector(
        ISortField field) =>
        new(string.Format(
            CultureInfo.CurrentCulture,
            DataResources.Sorting_InvalidState_ParentIsNoFieldSelector,
            field.DeclaringType.Print(),
            field.Name,
            field.Type.Print()));

    public static InvalidOperationException QueryableSorting_MemberInvalid(
        MemberInfo memberInfo,
        ISortField field) =>
        new(string.Format(
            CultureInfo.CurrentCulture,
            DataResources.QueryableSorting_MemberInvalid,
            memberInfo.GetType(),
            field.DeclaringType.Print(),
            field.Name,
            field.Type.Print()));

    public static InvalidOperationException QueryableSorting_ExpressionParameterInvalid(
        Type type,
        ISortField field) =>
        new(string.Format(
            CultureInfo.CurrentCulture,
            DataResources.QueryableSorting_ExpressionParameterInvalid,
            type.FullName,
            field.DeclaringType.Print(),
            field.Name,
            field.Type.Print()));

    public static SchemaException QueryableSortProvider_ExpressionParameterInvalid(
        ITypeSystemObject type,
        ISortInputTypeDefinition typeDefinition,
        ISortFieldDefinition field) =>
        new(SchemaErrorBuilder
            .New()
            .SetMessage(
                DataResources.QueryableSortProvider_ExpressionParameterInvalid,
                typeDefinition.EntityType?.FullName,
                field.Name)
            .SetTypeSystemObject(type)
            .Build());

    public static InvalidOperationException QueryableSorting_NoMemberDeclared(ISortField field) =>
        new(string.Format(
            CultureInfo.CurrentCulture,
            DataResources.QueryableSorting_NoMemberDeclared,
            field.DeclaringType.Print(),
            field.Name,
            field.Type.Print()));

    public static InvalidOperationException SortField_ArgumentInvalid_NoHandlerWasFound() =>
        new(string.Format(
            CultureInfo.CurrentCulture,
            DataResources.SortField_ArgumentInvalid_NoHandlerWasFound));

    public static InvalidOperationException ProjectionVisitor_CouldNotUnwrapType(IType type) =>
        new(string.Format(
            CultureInfo.CurrentCulture,
            DataResources.ProjectionVisitor_CouldNotUnwrapType,
            type.Print()));

    public static GraphQLException GlobalIdInputValueFormatter_SpecifiedValueIsNotAValidId()
        => new(ErrorBuilder
            .New()
            .SetMessage(DataResources.GlobalIdInputValueFormatter_SpecifiedValueIsNotAValidId)
            .Build());

    public static GraphQLException GlobalIdInputValueFormatter_IdsHaveInvalidFormat(
        IEnumerable<string?> ids)
        => new(ErrorBuilder.New()
            .SetMessage(
                "The IDs `{0}` have an invalid format.",
                string.Join(", ", ids))
            .Build());

    public static InvalidOperationException SelectionContext_NoTypeForAbstractFieldProvided(
        INamedType type,
        IEnumerable<ObjectType> possibleTypes) =>
        new(string.Format(
            CultureInfo.CurrentCulture,
            DataResources.SelectionContext_NoTypeForAbstractFieldProvided,
            type.NamedType().Name,
            string.Join(",", possibleTypes.Select(x => x.NamedType().Name))));
}
