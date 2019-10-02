using System;
using System.Collections.Generic;
using System.Text;

namespace HotChocolate.Types.Filters
{
    public interface IArrayBooleanFilterOperationDescriptor : IBooleanFilterOperationDescriptorBase
    {
        /// <summary>
        /// Define filter operations for another field.
        /// </summary>
        IArrayFilterFieldDescriptor And();
    }
}
