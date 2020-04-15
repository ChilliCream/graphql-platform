using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Sorting.Conventions
{
    public interface ISortingConvention : IConvention
    {
        /// <summary>
        /// Get the argument name definined for
        /// <see cref="SortObjectFieldDescriptorExtensions.UseSorting(IObjectFieldDescriptor)"/>
        /// Can be configured with <see 
        ///     cref="ISortingConventionDescriptor.ArgumentName(NameString)"/>
        /// </summary>
        NameString GetArgumentName();

        /// <summary>
        /// Get the graphql name of the enum value  <see cref="SortOperationKind.Asc"/>
        /// </summary>
        NameString GetAscendingName();

        /// <summary>
        /// Get the graphql name of the enum value  <see cref="SortOperationKind.Desc"/>
        /// </summary>
        NameString GetDescendingName();

        /// <summary>
        /// Get a <see cref="NameString">GraphQL Name</see> for a <see cref="SortingInputType{T}"/>
        /// Can be configured with <see 
        ///     cref="ISortingConventionDescriptor.TypeName(GetSortingTypeName)"/>
        /// </summary>
        /// <param name="context">The descriptor context of the schema creation</param>
        /// <param name="entityType">The type <c>{T}</c> of the <see cref="SortingInputType{T}"/>
        /// </param>
        NameString GetTypeName(IDescriptorContext context, Type entityType);

        /// <summary>
        /// Get a the description for a <see cref="SortingInputType{T}"/>
        /// Can be configured with <see 
        ///     cref="ISortingConventionDescriptor.Description(GetSortingDescription)"/>
        /// </summary>
        /// <param name="context">The descriptor context of the schema creation</param>
        /// <param name="entityType">The type <c>{T}</c> of the <see cref="SortingInputType{T}"/>
        /// </param>
        string GetDescription(IDescriptorContext context, Type entityType);

        /// <summary>
        /// Get a <see cref="NameString">GraphQL Name</see> for an operation of a
        /// <see cref="SortingInputType{T}"/>
        /// Can be configured with <see 
        ///     cref="ISortingConventionDescriptor.TypeName(GetSortingTypeName)"/>
        /// </summary>
        /// <param name="context">The descriptor context of the schema creation</param>
        /// <param name="entityType">The type <c>{T}</c> of the <see cref="SortingInputType{T}"/>
        /// </param>
        NameString GetOperationKindTypeName(IDescriptorContext context, Type entityType);

        /// <summary>
        /// Gets a list of all possible <see cref="TryCreateImplicitSorting"/>. This can be used to
        /// create <see cref="SortInputType{T}"/> implicitly
        /// Can be configured with
        /// <see cref="ISortingConventionDescriptor.AddImplicitSorting(TryCreateImplicitSorting)"/>
        /// </summary>
        /// <returns></returns>
        IReadOnlyList<TryCreateImplicitSorting> GetImplicitFactories();

        Task ApplySorting<T>(
            FieldDelegate next,
            ITypeConversion converter,
            IMiddlewareContext context);

        bool TryGetVisitorDefinition<T>(
            [NotNullWhen(true)]out T? definition)
            where T : SortingVisitorDefinitionBase;
    }
}
