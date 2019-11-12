using System;
using HotChocolate.Language;

namespace HotChocolate.Types.Sorting
{
    public interface ISortObjectOperationDescriptor<TObject>
        : IDescriptor<SortOperationDefintion>
        , IFluent
    {
        /// <summary>
        /// Specify the name of the sort operation.
        /// </summary>
        /// <param name="value">
        ///  The sort operation name.
        /// </param>
        ISortObjectOperationDescriptor<TObject> Name(NameString value);

        /// <summary>
        /// Ignore the specified property.
        /// </summary>
        /// <param name="property">The property that shall be ignored.</param>
        ISortObjectOperationDescriptor<TObject> Ignore();

        /// <summary>
        /// Specifies the type of the filter operation
        /// </summary>
        /// <param name="descriptor"></param>
        /// <returns></returns>
        ISortObjectOperationDescriptor<TObject> Type(
            Action<ISortInputTypeDescriptor<TObject>> descriptor);

        /// <summary>
        /// Specifies the type of the filter operation
        /// </summary>
        ISortObjectOperationDescriptor<TObject> Type<TFilter>()
            where TFilter : SortInputType<TObject>;

        /// <summary>
        /// Specify the description of the filter operation.
        /// </summary>
        /// <param name="value">
        ///  The operation description.
        /// </param>
        ISortObjectOperationDescriptor<TObject> Description(string value);

        /// <summary>
        /// Annotate the operation filter field with a directive.
        /// </summary>
        /// <param name="directiveInstance">
        /// The directive with which the field shall be annotated.
        /// </param>
        /// <typeparam name="T">
        /// The directive type.
        /// </typeparam>
        ISortObjectOperationDescriptor<TObject> Directive<T>(T directiveInstance)
            where T : class;

        /// <summary>
        /// Annotate the operation filter field with a directive.
        /// </summary>
        /// <typeparam name="T">
        /// The directive type.
        /// </typeparam>
        ISortObjectOperationDescriptor<TObject> Directive<T>()
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
        ISortObjectOperationDescriptor<TObject> Directive(
            NameString name,
            params ArgumentNode[] arguments);
    }
}
