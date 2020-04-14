using System;

namespace HotChocolate.Types.Sorting.Conventions
{
    public interface ISortingConventionDescriptor : IFluent
    {
        /// <summary>
        /// Defines the argument name of the filter used by
        /// <see cref="SortObjectFieldDescriptorExtensions.UseSorting"/>
        /// </summary>
        /// <param name="argumentName">The argument name.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="argumentName"/> is <c>null</c> or  <see cref="string.Empty"/>.
        /// </exception>
        ISortingConventionDescriptor ArgumentName(NameString argumentName);

        /// <summary>
        /// Specifies a delegate that returns the graphql type name of a
        /// <see cref="SortInputType{T}"/>
        /// </summary>
        /// <param name="factory">The delegate to name the type</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="factory"/> is <c>null</c>
        /// </exception>
        ISortingConventionDescriptor TypeName(
            GetSortingTypeName factory);

        /// <summary>
        /// Specifies a delegate that returns the graphql description of a
        /// <see cref="SortInputType{T}"/>
        /// </summary>
        /// <param name="factory">The delegate to describe the type</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="factory"/> is <c>null</c>
        /// </exception>
        ISortingConventionDescriptor Description(
            GetSortingDescription factory);

        /// <summary>
        /// Specifies a delegate that returns the graphql type name of a an operation of a
        /// <see cref="SortInputType{T}"/>
        /// </summary>
        /// <param name="factory">The delegate to name the type</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="factory"/> is <c>null</c>
        /// </exception>
        ISortingConventionDescriptor OperationKindTypeName(
            GetSortingTypeName factory);

        /// <summary>
        /// Defines the graphql name of the enum value  <see cref="SortOperationKind.Asc"/>
        /// </summary>
        /// <param name="valueName">The value name.</param>
        /// <exception cref="ArgumentException">
        /// <paramref name="valueName"/> is <c>null</c> or  <see cref="string.Empty"/>.
        /// </exception>
        ISortingConventionDescriptor AscendingName(NameString valueName);

        /// <summary>
        /// Defines the graphql name of the enum value  <see cref="SortOperationKind.Desc"/>
        /// </summary>
        /// <param name="valueName">The value name.</param>
        /// <exception cref="ArgumentException">
        /// <paramref name="valueName"/> is <c>null</c> or  <see cref="string.Empty"/>.
        /// </exception>
        ISortingConventionDescriptor DescendingName(NameString valueName);

        /// <summary>
        /// Specifies a delegate of type <see cref="AddImplicitSorting"/>. This delegate is
        /// invoked when ever sorting is implicitly bound. <seealso cref="BindingBehavior"/>
        /// </summary>
        /// <param name="factory">The factory to create implicit filters</param>
        /// <param name="position" default="null">
        /// At this position the factory is inserted. Default is at the end.
        /// </param>
        ISortingConventionDescriptor AddImplicitSorting(
            TryCreateImplicitSorting factory, int? position = null);

        /// <summary>
        /// Configures the visitor that is used to process sorting
        /// </summary>
        /// <param name="visitor">The <see cref="SortingVisitorDescriptorBase<T>"/></param>
        ISortingConventionDescriptor Visitor(
            ISortingVisitorDescriptorBase<SortingVisitorDefinitionBase> visitor);

        /// <summary>
        /// Removes all configuration (including default configuration!)
        /// Apply <see cref="SortingConventionDescriptorExtensions.UseDefault"/>
        /// to add the defaults.
        /// </summary>
        ISortingConventionDescriptor Reset();
    }
}
