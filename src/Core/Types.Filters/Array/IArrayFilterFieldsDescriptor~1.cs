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
        /// The string filter field descriptor will add
        /// all available string filter operations.
        /// </summary>
        IArrayFilterFieldDescriptor<TArray> BindImplicitly();

        /// <summary>
        /// Allow object filter operations.
        /// </summary>
        IArrayFilterOperationDescriptor<TArray> AllowSome(Action<IFilterInputTypeDescriptor<TArray>> descriptor);


        /// <summary>
        /// Allow object filter operations.
        /// </summary>
        IArrayFilterOperationDescriptor<TArray> AllowSome<TFilter>() where TFilter : FilterInputType<TArray>;

        /// <summary>
        /// Allow object filter operations.
        /// </summary>
        IArrayFilterOperationDescriptor<TArray> AllowSome();

    }
}
