using System;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types.Filters
{
    public interface IArrayFilterFieldDescriptor<TArray>
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
        IArrayFilterFieldDescriptor<TArray> BindFilters(
            BindingBehavior bindingBehavior);

        /// <summary>
        /// Defines that all filter operations have to be specified explicitly.
        /// </summary>
        IArrayFilterFieldDescriptor<TArray> BindExplicitly();

        /// <summary>
        /// The array filter field descriptor will add
        /// all available array filter operations.
        /// </summary>
        IArrayFilterFieldDescriptor<TArray> BindImplicitly();

        /// <summary>
        /// Allow array filter operations. Some returns true wehn one item matchs the filter
        /// </summary>
        IArrayFilterOperationDescriptor<TArray> AllowSome(
            Action<IFilterInputTypeDescriptor<TArray>> descriptor);

        /// <summary>
        /// Allow array filter operations. Some returns true wehn one item matchs the filter
        /// </summary>
        IArrayFilterOperationDescriptor<TArray> AllowSome<TFilter>() 
            where TFilter : FilterInputType<TArray>;

        /// <summary>
        /// Allow array filter operations. Some returns true wehn one item matchs the filter
        /// </summary>
        IArrayFilterOperationDescriptor<TArray> AllowSome();
        /// <summary>
        /// Allow array filter operations. None returns true when no item matchs the filter
        /// </summary>
        IArrayFilterOperationDescriptor<TArray> AllowNone(
            Action<IFilterInputTypeDescriptor<TArray>> descriptor);

        /// <summary>
        /// Allow array filter operations. None returns true when no item matchs the filter
        /// </summary>
        IArrayFilterOperationDescriptor<TArray> AllowNone<TFilter>() 
            where TFilter : FilterInputType<TArray>;

        /// <summary>
        /// Allow array filter operations. None returns true when no item matchs the filter
        /// </summary>
        IArrayFilterOperationDescriptor<TArray> AllowNone();
        /// <summary>
        /// Allow array filter operations. All returns true when all item match the filter
        /// </summary>
        IArrayFilterOperationDescriptor<TArray> AllowAll(
            Action<IFilterInputTypeDescriptor<TArray>> descriptor);

        /// <summary>
        /// Allow array filter operations. All returns true when all item match the filter
        /// </summary>
        IArrayFilterOperationDescriptor<TArray> AllowAll<TFilter>() 
            where TFilter : FilterInputType<TArray>;

        /// <summary>
        /// Allow array filter operations. All returns true when all item match the filter
        /// </summary>
        IArrayFilterOperationDescriptor<TArray> AllowAll();

        /// <summary>
        /// Allow array filter operations. Check if there are items or there are none
        /// </summary>
        IArrayBooleanFilterOperationDescriptor AllowAny();
    }
}
