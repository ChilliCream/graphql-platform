using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types.Filters
{
    public interface IBooleanFilterFieldDescriptor
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
        IBooleanFilterFieldDescriptor BindFilters(
            BindingBehavior bindingBehavior);

        /// <summary>
        /// Defines that all filter operations have to be specified explicitly.
        /// </summary>
        IBooleanFilterFieldDescriptor BindFiltersExplicitly();

        /// <summary>
        /// The boolean filter field descriptor will add
        /// all available boolean filter operations.
        /// </summary>
        IBooleanFilterFieldDescriptor BindFiltersImplicitly();

        /// <summary>
        /// Allow equals filter operations.
        /// </summary>
        IBooleanFilterOperationDescriptor AllowEquals();

        /// <summary>
        /// Allow not equals filter operations.
        /// </summary>
        IBooleanFilterOperationDescriptor AllowNotEquals();

        /// <summary>
        /// Ignore the specified property.
        /// </summary>
        /// <param name="property">The property that hall be ignored.</param>
        IBooleanFilterFieldDescriptor Ignore();
    }
}
