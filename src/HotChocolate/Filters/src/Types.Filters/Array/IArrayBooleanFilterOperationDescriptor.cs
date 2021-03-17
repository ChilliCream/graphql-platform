using System;

namespace HotChocolate.Types.Filters
{
    [Obsolete("Use HotChocolate.Data.")]
    public interface IArrayBooleanFilterOperationDescriptor
        : IBooleanFilterOperationDescriptorBase
    {
        /// <summary>
        /// Define filter operations for another field.
        /// </summary>
        IArrayFilterFieldDescriptor And();
    }
}
