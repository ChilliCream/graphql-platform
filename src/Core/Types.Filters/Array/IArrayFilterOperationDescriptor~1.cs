using HotChocolate.Language;

namespace HotChocolate.Types.Filters
{
    public interface IArrayFilterOperationDescriptor<TArray>
        : IDescriptor<FilterOperationDefintion>
        , IFluent
    {
        /// <summary>
        /// Define filter operations for another field.
        /// </summary>
        IArrayFilterFieldDescriptor<TArray> And();

        /// <summary>
        /// Specify the name of the filter operation.
        /// </summary>
        /// <param name="value">
        ///  The operation name.
        /// </param>
        IArrayFilterOperationDescriptor<TArray> Name(NameString value);

        /// <summary>
        /// Specify the description of the filter operation.
        /// </summary>
        /// <param name="value">
        ///  The operation description.
        /// </param>
        IArrayFilterOperationDescriptor<TArray> Description(string value);

        /// <summary>
        /// Annotate the operation filter field with a directive.
        /// </summary>
        /// <param name="directiveInstance">
        /// The directive with which the field shall be annotated.
        /// </param>
        /// <typeparam name="T">
        /// The directive type.
        /// </typeparam>
        IArrayFilterOperationDescriptor<TArray> Directive<T>(T directiveInstance)
            where T : class;

        /// <summary>
        /// Annotate the operation filter field with a directive.
        /// </summary>
        /// <typeparam name="T">
        /// The directive type.
        /// </typeparam>
        IArrayFilterOperationDescriptor<TArray> Directive<T>()
            where T : class, new();

        /// <summary>
        /// Annotate the operation filter field with a directive.
        /// </summary>
        /// <param name="name">
        /// The name of the directive.
        /// </param>
        /// <param name="arguments">
        /// The argument values of the directive.
        /// </param>
        IArrayFilterOperationDescriptor<TArray> Directive(
            NameString name,
            params ArgumentNode[] arguments);
    }
}
