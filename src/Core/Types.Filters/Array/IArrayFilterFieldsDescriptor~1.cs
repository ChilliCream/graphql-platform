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
        /// <summary>
        /// Allow object filter operations.
        /// </summary>
        IArrayFilterOperationDescriptor<TArray> AllowNone(Action<IFilterInputTypeDescriptor<TArray>> descriptor);


        /// <summary>
        /// Allow object filter operations.
        /// </summary>
        IArrayFilterOperationDescriptor<TArray> AllowNone<TFilter>() where TFilter : FilterInputType<TArray>;

        /// <summary>
        /// Allow object filter operations.
        /// </summary>
        IArrayFilterOperationDescriptor<TArray> AllowNone();
        /// <summary>
        /// Allow object filter operations.
        /// </summary>
        IArrayFilterOperationDescriptor<TArray> AllowAll(Action<IFilterInputTypeDescriptor<TArray>> descriptor);


        /// <summary>
        /// Allow object filter operations.
        /// </summary>
        IArrayFilterOperationDescriptor<TArray> AllowAll<TFilter>() where TFilter : FilterInputType<TArray>;

        /// <summary>
        /// Allow object filter operations.
        /// </summary>
        IArrayFilterOperationDescriptor<TArray> AllowAll();

        /// <summary>
        /// Allow object filter operations.
        /// </summary>
        IArrayBooleanFilterOperationDescriptor AllowAny();

    }
}
