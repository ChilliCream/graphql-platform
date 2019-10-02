using System;
using System.Collections.Generic;
using System.Text;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types.Filters
{
    public class ArrayBooleanFilterOperationDescriptor : BooleanFilterOperationDescriptorBase, IArrayBooleanFilterOperationDescriptor
    {
        private readonly ArrayFilterFieldDescriptor _descriptor;

        protected ArrayBooleanFilterOperationDescriptor(
            IDescriptorContext context,
            ArrayFilterFieldDescriptor descriptor,
            NameString name,
            ITypeReference type,
            FilterOperation operation) : base(context, name, type, operation)
        {
            _descriptor = descriptor
                ?? throw new ArgumentNullException(nameof(descriptor));
        }

        public IArrayFilterFieldDescriptor And()
        {
            return _descriptor;
        }



        /// <summary>
        /// Create a new boolean filter operation descriptor.
        /// </summary>
        /// <param name="context">
        /// The descriptor context.
        /// </param>
        /// <param name="descriptor">
        /// The field descriptor on which this
        /// filter operation shall be applied.
        /// </param>
        /// <param name="name">
        /// The default name of the filter operation field.
        /// </param>
        /// <param name="type">
        /// The field type of this filter operation field.
        /// </param>
        /// <param name="operation">
        /// The filter operation info.
        /// </param>
        public static ArrayBooleanFilterOperationDescriptor New(
            IDescriptorContext context,
            ArrayFilterFieldDescriptor descriptor,
            NameString name,
            ITypeReference type,
            FilterOperation operation) =>
            new ArrayBooleanFilterOperationDescriptor(
                context, descriptor, name, type, operation);
    }
}
