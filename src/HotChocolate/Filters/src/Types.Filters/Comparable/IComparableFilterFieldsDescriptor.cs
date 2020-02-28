using System;
using HotChocolate.Language;

namespace HotChocolate.Types.Filters
{
    public interface IComparableFilterFieldDescriptor
        : IFluent
    {
        /// <summary>
        /// Defines the filter binding behavior.
        ///
        /// The default binding behavior is set to
        /// <see cref="BindingBehavior.Implicit"/>.
        /// </summary>
        /// <param name="behavior">
        /// The binding behavior.
        ///
        /// Implicit:
        /// The boolean filter field descriptor will add
        /// all available boolean filter operations.
        ///
        /// Explicit:
        /// All filter operations have to be specified explicitly.
        /// </param>
        IComparableFilterFieldDescriptor BindFilters(
            BindingBehavior bindingBehavior);

        /// <summary>
        /// Defines that all filter operations have to be specified explicitly.
        /// </summary>
        IComparableFilterFieldDescriptor BindFiltersExplicitly();

        /// <summary>
        /// The comparable filter field descriptor will add
        /// all available comparable filter operations.
        /// </summary>
        IComparableFilterFieldDescriptor BindFiltersImplicitly();

        /// <summary>
        /// Specifies the GraphQL leaf type for this filter field.
        /// </summary>
        /// <typeparam name="TInputType">The GraphQL leaf type.</typeparam>
        IComparableFilterFieldDescriptor Type<TLeafType>()
            where TLeafType : class, ILeafType;

        /// <summary>
        /// Specifies the GraphQL leaf type for this filter field.
        /// </summary>
        /// <param name="inputType">The GraphQL leaf type instance.</param>
        /// <typeparam name="TInputType">The GraphQL leaf type.</typeparam>
        IComparableFilterFieldDescriptor Type<TLeafType>(TLeafType inputType)
            where TLeafType : class, ILeafType;

        /// <summary>
        /// Specifies the GraphQL leaf type for this filter field.
        /// </summary>
        /// <param name="type">The GraphQL leaf type.</param>
        IComparableFilterFieldDescriptor Type(Type type);

        /// <summary>
        /// Specifies the GraphQL leaf type for this filter field.
        /// </summary>
        /// <param name="typeNode">The GraphQL leaf type reference.</param>
        IComparableFilterFieldDescriptor Type(NamedTypeNode typeNode);

        /// <summary>
        /// Allow equals filter operations.
        /// </summary>
        IComparableFilterOperationDescriptor AllowEquals();

        /// <summary>
        /// Allow not equals filter operations.
        /// </summary>
        IComparableFilterOperationDescriptor AllowNotEquals();

        /// <summary>
        /// Allow in filter operations.
        /// </summary>
        IComparableFilterOperationDescriptor AllowIn();

        /// <summary>
        /// Allow not in filter operations.
        /// </summary>
        IComparableFilterOperationDescriptor AllowNotIn();

        /// <summary>
        /// Allow greater than filter operations.
        /// </summary>
        IComparableFilterOperationDescriptor AllowGreaterThan();

        /// <summary>
        /// Allow not greater than filter operations.
        /// </summary>
        IComparableFilterOperationDescriptor AllowNotGreaterThan();

        /// <summary>
        /// Allow greater than or equals filter operations.
        /// </summary>
        IComparableFilterOperationDescriptor AllowGreaterThanOrEquals();

        /// <summary>
        /// Allow not greater than or equals filter operations.
        /// </summary>
        IComparableFilterOperationDescriptor AllowNotGreaterThanOrEquals();

        /// <summary>
        /// Allow lower than filter operations.
        /// </summary>
        IComparableFilterOperationDescriptor AllowLowerThan();

        /// <summary>
        /// Allow not lower than filter operations.
        /// </summary>
        IComparableFilterOperationDescriptor AllowNotLowerThan();

        /// <summary>
        /// Allow lower than or equals filter operations.
        /// </summary>
        IComparableFilterOperationDescriptor AllowLowerThanOrEquals();

        /// <summary>
        /// Allow not lower than or equals filter operations.
        /// </summary>
        IComparableFilterOperationDescriptor AllowNotLowerThanOrEquals();

        /// <summary>
        /// Ignore the specified property.
        /// </summary>
        /// <param name="ignore">If set to true the field is ignored</param>
        IComparableFilterFieldDescriptor Ignore(bool ignore = true);
    }
}
