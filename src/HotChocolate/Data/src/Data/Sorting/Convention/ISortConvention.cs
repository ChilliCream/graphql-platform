using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using HotChocolate.Configuration;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Data.Sorting;

/// <summary>
/// The sort convention provides defaults for inferring sorting fields.
/// </summary>
public interface ISortConvention : IConvention
{
    /// <summary>
    /// Gets the GraphQL type name from a runtime type.
    /// </summary>
    /// <param name="runtimeType">
    /// The runtime type.
    /// </param>
    /// <returns>
    /// Returns the GraphQL type name that was inferred from the <paramref name="runtimeType"/>.
    /// </returns>
    string GetTypeName(Type runtimeType);

    /// <summary>
    /// Gets the GraphQL type description from a runtime type.
    /// </summary>
    /// <param name="runtimeType">
    /// The runtime type.
    /// </param>
    /// <returns>
    /// Returns the GraphQL type description that was
    /// inferred from the <paramref name="runtimeType"/>.
    /// </returns>
    string? GetTypeDescription(Type runtimeType);

    /// <summary>
    /// Gets the GraphQL field name from a <see cref="MemberInfo"/>.
    /// </summary>
    /// <param name="member">
    /// The member from which a field shall be inferred.
    /// </param>
    /// <returns>
    /// Returns the GraphQL field name that was inferred from the <see cref="MemberInfo"/>.
    /// </returns>
    string GetFieldName(MemberInfo member);

    /// <summary>
    /// Gets the GraphQL field description from a <see cref="MemberInfo"/>.
    /// </summary>
    /// <param name="member">
    /// The member from which a field shall be inferred.
    /// </param>
    /// <returns>
    /// Returns the GraphQL field description that was inferred from the
    /// <see cref="MemberInfo"/>.
    /// </returns>
    string? GetFieldDescription(MemberInfo member);

    /// <summary>
    /// Extracts the field type from a <see cref="MemberInfo"/>.
    /// </summary>
    /// <param name="member">
    /// The member from which a field shall be inferred.
    /// </param>
    /// <returns>
    /// Returns a <see cref="ExtendedTypeReference"/> that represents the field type.
    /// </returns>
    ExtendedTypeReference GetFieldType(MemberInfo member);

    /// <summary>
    /// Gets the operation name for the provided <paramref name="operationId"/>.
    /// </summary>
    /// <param name="operationId">
    /// The internal operation ID.
    /// </param>
    /// <returns>
    /// Returns the operation name.
    /// </returns>
    string GetOperationName(int operationId);

    /// <summary>
    /// Gets the operation description for the provided <paramref name="operationId"/>.
    /// </summary>
    /// <param name="operationId">
    /// The internal operation ID.
    /// </param>
    /// <returns>
    /// Returns the operation description.
    /// </returns>
    string? GetOperationDescription(int operationId);

    /// <summary>
    /// Gets the sort argument name.
    /// </summary>
    /// <returns>
    /// Returns the sort argument name.
    /// </returns>
    string GetArgumentName();

    /// <summary>
    /// Applies configurations to a sort type.
    /// </summary>
    /// <param name="typeReference">
    /// The type reference representing the type.
    /// </param>
    /// <param name="descriptor">
    /// The descriptor to which the configurations shall be applied to.
    /// </param>
    void ApplyConfigurations(
        TypeReference typeReference,
        ISortInputTypeDescriptor descriptor);

    /// <summary>
    /// Applies configurations to a sort enum type.
    /// </summary>
    /// <param name="typeReference">
    /// The type reference representing the enum type.
    /// </param>
    /// <param name="descriptor">
    /// The descriptor to which the configurations shall be applied to.
    /// </param>
    void ApplyConfigurations(
        TypeReference typeReference,
        ISortEnumTypeDescriptor descriptor);

    bool TryGetFieldHandler(
        ITypeCompletionContext context,
        ISortInputTypeDefinition typeDefinition,
        ISortFieldDefinition fieldDefinition,
        [NotNullWhen(true)] out ISortFieldHandler? handler);

    bool TryGetOperationHandler(
        ITypeCompletionContext context,
        EnumTypeDefinition typeDefinition,
        SortEnumValueDefinition fieldDefinition,
        [NotNullWhen(true)] out ISortOperationHandler? handler);

    /// <summary>
    /// Creates a middleware that represents the sort execution logic
    /// for the specified entity type.
    /// </summary>
    /// <typeparam name="TEntityType">
    /// The entity type for which an sort executor shall be created.
    /// </typeparam>
    /// <returns>
    /// Returns a field middleware which represents the sort execution logic
    /// for the specified entity type.
    /// </returns>
    IQueryBuilder CreateBuilder<TEntityType>();

    /// <summary>
    /// Configures the field where sorting is applied. This can be used to add context
    /// data to the field.
    /// </summary>
    /// <param name="fieldDescriptor">
    /// the field descriptor where the sorting is applied
    /// </param>
    void ConfigureField(IObjectFieldDescriptor fieldDescriptor);

    /// <summary>
    /// Creates metadata for a field that the provider can pick up an use for the translation
    /// </summary>
    ISortMetadata? CreateMetaData(
        ITypeCompletionContext context,
        ISortInputTypeDefinition typeDefinition,
        ISortFieldDefinition fieldDefinition);
}
