using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types.Filters
{
    public interface IStringFilterFieldDescriptor
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
        IStringFilterFieldDescriptor BindFilters(
            BindingBehavior bindingBehavior);

        /// <summary>
        /// Defines that all filter operations have to be specified explicitly.
        /// </summary>
        IStringFilterFieldDescriptor BindFiltersExplicitly();

        /// <summary>
        /// The string filter field descriptor will add
        /// all available string filter operations.
        /// </summary>
        IStringFilterFieldDescriptor BindFiltersImplicitly();

        /// <summary>
        /// Allow contains filter operations.
        /// </summary>
        IStringFilterOperationDescriptor AllowContains();

        /// <summary>
        /// Allow not contains filter operations.
        /// </summary>
        IStringFilterOperationDescriptor AllowNotContains();

        /// <summary>
        /// Allow equals filter operations.
        /// </summary>
        IStringFilterOperationDescriptor AllowEquals();

        /// <summary>
        /// Allow not equals filter operations.
        /// </summary>
        IStringFilterOperationDescriptor AllowNotEquals();

        /// <summary>
        /// Allow in filter operations.
        /// </summary>
        IStringFilterOperationDescriptor AllowIn();

        /// <summary>
        /// Allow not in filter operations.
        /// </summary>
        IStringFilterOperationDescriptor AllowNotIn();

        /// <summary>
        /// Allow starts with filter oprerations.
        /// </summary>
        IStringFilterOperationDescriptor AllowStartsWith();

        /// <summary>
        /// Allow not starts with filter oprerations.
        /// </summary>
        IStringFilterOperationDescriptor AllowNotStartsWith();

        /// <summary>
        /// Allow ends with filter oprerations.
        /// </summary>
        IStringFilterOperationDescriptor AllowEndsWith();

        /// <summary>
        /// Allow not ends with filter oprerations.
        /// </summary>
        IStringFilterOperationDescriptor AllowNotEndsWith();

        /// <summary>
        /// Ignore the specified property.
        /// </summary>
        /// <param name="property">The property that hall be ignored.</param>
        IStringFilterFieldDescriptor Ignore();
    }
}
