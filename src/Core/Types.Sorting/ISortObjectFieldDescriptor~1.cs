using System;

namespace HotChocolate.Types.Sorting
{
    public interface ISortObjectFieldDescriptor<TObject> 
    {
        /// <summary>
        /// Specify the name of the sort operation.
        /// </summary>
        /// <param name="value">
        ///  The sort operation name.
        /// </param>
        ISortObjectFieldDescriptor<TObject> Name(NameString value);

        /// <summary>
        /// Ignore the specified property.
        /// </summary>
        /// <param name="property">The property that shall be ignored.</param>

        ISortObjectFieldDescriptor<TObject> Ignore();



        /// <summary>
        /// Allow object filter operations.
        /// </summary>
        ISortObjectFieldDescriptor<TObject> AllowSort(
            Action<ISortInputTypeDescriptor<TObject>> descriptor);


        /// <summary>
        /// Allow object filter operations.
        /// </summary>
        ISortObjectFieldDescriptor<TObject> AllowSort<TFilter>()
            where TFilter : SortInputType<TObject>;

        /// <summary>
        /// Allow object filter operations.
        /// </summary>
        ISortObjectFieldDescriptor<TObject> AllowSort();
    }
}
