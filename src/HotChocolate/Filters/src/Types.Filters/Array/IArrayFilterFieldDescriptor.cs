using System;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types.Filters
{
    public interface IArrayFilterFieldDescriptor
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
        IArrayFilterFieldDescriptor BindFilters(
            BindingBehavior bindingBehavior);

        /// <summary>
        /// Defines that all filter operations have to be specified explicitly.
        /// </summary>
        IArrayFilterFieldDescriptor BindExplicitly();

        /// <summary>
        /// The string filter field descriptor will add
        /// all available string filter operations.
        /// </summary>
        IArrayFilterFieldDescriptor BindImplicitly();

        /// <summary>
        /// Allow array filter operations. Some returns true wehn one item matchs the filter
        /// </summary>
        IArrayFilterOperationDescriptor AllowSome();
        /// <summary>
        /// Allow array filter operations. None returns true when no item matchs the filter
        /// </summary>
        IArrayFilterOperationDescriptor AllowNone();
        /// <summary>
        /// Allow array filter operations. All returns true when all item match the filter
        /// </summary>
        IArrayFilterOperationDescriptor AllowAll();
        /// <summary>
        /// Allow array filter operations. Check if there are items or there are none
        /// </summary>
        IArrayBooleanFilterOperationDescriptor AllowAny();

    }
}
