using System;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types.Filters
{
    public interface IObjectFilterFieldDescriptor
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
        IObjectFilterFieldDescriptor BindFilters(
            BindingBehavior bindingBehavior);

        /// <summary>
        /// Defines that all filter operations have to be specified explicitly.
        /// </summary>
        IObjectFilterFieldDescriptor BindExplicitly();

        /// <summary>
        /// The string filter field descriptor will add
        /// all available string filter operations.
        /// </summary>
        IObjectFilterFieldDescriptor BindImplicitly();

    }

    public interface IObjectFilterFieldDescriptor<TObject>
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
        IObjectFilterFieldDescriptor<TObject> BindFilters(
            BindingBehavior bindingBehavior);

        /// <summary>
        /// Defines that all filter operations have to be specified explicitly.
        /// </summary>
        IObjectFilterFieldDescriptor<TObject> BindExplicitly();

        /// <summary>
        /// The string filter field descriptor will add
        /// all available string filter operations.
        /// </summary>
        IObjectFilterFieldDescriptor<TObject> BindImplicitly();

        /// <summary>
        /// Allow object filter operations.
        /// </summary>
        IObjectFilterFieldDescriptor<TObject> AllowObject(Action<IFilterInputTypeDescriptor<TObject>> descriptor);


        /// <summary>
        /// Allow object filter operations.
        /// </summary>
        IObjectFilterFieldDescriptor<TObject> AllowObject<TFilter>() where TFilter : FilterInputType<TObject>;

    }
}
