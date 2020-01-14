using System;
using System.Collections.Generic;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types.Filters.Conventions
{
    public interface IFilterConvention : IConvention
    {
        /// <summary>
        /// Get the argument name definined for
        /// <see cref="FilterObjectFieldDescriptorExtensions.UseFiltering"/>
        /// Can be configured over <see cref="IFilterConventionDescriptor.ArgumentName"/>
        /// </summary> 
        NameString GetArgumentName();

        /// <summary>
        /// Get the base name of the element in array filters.
        /// Can be configured over
        /// <see cref="IFilterConventionDescriptor.ElementName(NameString)"/>
        /// </summary> 
        NameString GetArrayFilterPropertyName();

        /// <summary>
        /// Create a field name for a filter operation.
        /// Checks first for specific naming conventions over
        /// <c>FilterConventionDescriptor.Type(FilterKind).Operation(FilterOperationKind)</c>
        /// Then checks for default configurations over
        /// <c>FilterConventionDescriptor.Operation(FilterOperationKind)</c>
        /// </summary>
        /// <param name="definition">The <see cref="FilterFieldDefintion"/> of the filter</param>
        /// <param name="kind">The <see cref="FilterOperationKind"/> for which the field name
        /// should be created</param> 
        NameString CreateFieldName(
            FilterFieldDefintion definition,
            FilterOperationKind kind);

        /// <summary>
        /// Gets the description of a field for a filter operation.
        /// Checks first for specific naming conventions over
        /// <c>FilterConventionDescriptor.Type(FilterKind).Operation(FilterOperationKind)</c>
        /// Then checks for default configurations over
        /// <c>FilterConventionDescriptor.Operation(FilterOperationKind)</c>
        /// </summary>
        /// <param name="operation">The <see cref="FilterOperation"/> that the description
        /// is based on</param>
        string GetOperationDescription(FilterOperation operation);

        /// <summary>
        /// Get configured <see cref="FilterOperationKind"/> that are valid for the definition
        /// This set contains all not ignored operations configured with
        /// <see cref="FilterConventionDescriptor.Operation(FilterOperationKind)"/> or
        /// <see cref="FilterConventionTypeDescriptor.Operation(FilterOperationKind)"/>
        /// </summary>
        /// <param name="definition"></param> 
        ISet<FilterOperationKind> GetAllowedOperations(FilterFieldDefintion definition);

        /// <summary>
        /// Get a <see cref="NameString">GraphQL Name</see> for a <see cref="FilterInputType{T}"/>
        /// Can be configured over <see cref="FilterConventionDescriptor.FilterTypeName"/>
        /// </summary>
        /// <param name="context">The descriptor context of the schema creation</param>
        /// <param name="entityType">The type <c>{T}</c> of the <see cref="FilterInputType{T}"/>
        /// </param>
        NameString GetFilterTypeName(IDescriptorContext context, Type entityType);

        /// <summary>
        /// Gets a list of all possible <see cref="TryCreateImplicitFilter"/>. This can be used to
        /// create <see cref="FilterInputType{T}"/> implicitly
        /// Can be configured with
        /// <see cref="FilterConventionTypeDescriptor.TryCreateImplicitFilter"/>
        /// </summary>
        /// <returns></returns>
        IEnumerable<TryCreateImplicitFilter> GetImplicitFilterFactories();
    }

}
