using HotChocolate.Language;

namespace HotChocolate.Types.Sorting
{
    public interface ISortObjectOperationDescriptor
        : IDescriptor<SortOperationDefintion>
        , IFluent, ISortOperationDescriptor
    {
        /// <summary>
        /// Specify the name of the sort operation.
        /// </summary>
        /// <param name="value">
        ///  The sort operation name.
        /// </param>
        new ISortObjectOperationDescriptor Name(NameString value);

        /// <summary>
        /// Ignore the specified property.
        /// </summary>
        /// <param name="ignore">If set to true the field is ignored</param> 
        new ISortObjectOperationDescriptor Ignore(bool ignore = true);

        /// <summary>
        /// Specify the description of the filter operation.
        /// </summary>
        /// <param name="value">
        ///  The operation description.
        /// </param>
        new ISortObjectOperationDescriptor Description(string value);

        /// <summary>
        /// Annotate the operation filter field with a directive.
        /// </summary>
        /// <param name="directiveInstance">
        /// The directive with which the field shall be annotated.
        /// </param>
        /// <typeparam name="T">
        /// The directive type.
        /// </typeparam>
        new ISortObjectOperationDescriptor Directive<T>(T directiveInstance)
            where T : class;

        /// <summary>
        /// Annotate the operation filter field with a directive.
        /// </summary>
        /// <typeparam name="T">
        /// The directive type.
        /// </typeparam>
        new ISortObjectOperationDescriptor Directive<T>()
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
        new ISortObjectOperationDescriptor Directive(
            NameString name,
            params ArgumentNode[] arguments);
    }
}
