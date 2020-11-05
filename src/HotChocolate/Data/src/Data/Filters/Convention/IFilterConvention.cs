using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using HotChocolate.Configuration;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Data.Filters
{
    /// <summary>
    /// The filter convention provides defaults for inferring filters.
    /// </summary>
    public interface IFilterConvention : IConvention
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
        NameString GetTypeName(Type runtimeType);

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
        NameString GetFieldName(MemberInfo member);

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
        /// Returns a <see cref="RuntimeTypeReference"/> that represents the field type.
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
        NameString GetOperationName(int operationId);

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
        /// Gets the filter argument name.
        /// </summary>
        /// <returns>
        /// Returns the filter argument name.
        /// </returns>
        NameString GetArgumentName();

        /// <summary>
        /// Applies configurations to a filter type.
        /// </summary>
        /// <param name="typeReference">
        /// The type reference representing the type.
        /// </param>
        /// <param name="descriptor">
        /// The descriptor to which the configurations shall be applied to.
        /// </param>
        void ApplyConfigurations(
            ITypeReference typeReference,
            IFilterInputTypeDescriptor descriptor);

        bool TryGetHandler(
            ITypeDiscoveryContext context,
            IFilterInputTypeDefinition typeDefinition,
            IFilterFieldDefinition fieldDefinition,
            [NotNullWhen(true)] out IFilterFieldHandler? handler);

        /// <summary>
        /// Creates a middleware that represents the filter execution logic
        /// for the specified entity type.
        /// </summary>
        /// <typeparam name="TEntityType">
        /// The entity type for which an filter executor shall be created.
        /// </typeparam>
        /// <returns>
        /// Returns a field middleware which represents the filter execution logic
        /// for the specified entity type.
        /// </returns>
        FieldMiddleware CreateExecutor<TEntityType>();

        /// <summary>
        /// Configures the field where the filters are applied. This can be used to add context
        /// data to the field.
        /// </summary>
        /// <param name="fieldDescriptor">
        /// the field descriptor where the filtering is applied
        /// </param>
        void ConfigureField(IObjectFieldDescriptor fieldDescriptor);
    }
}
