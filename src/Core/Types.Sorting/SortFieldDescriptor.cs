using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types.Sorting
{
    public class SortFieldDescriptor
        : SortFieldDescriptorBase
        , ISortFieldDescriptor
    {
        public SortFieldDescriptor(IDescriptorContext context, PropertyInfo property) : base(context, property)
        {
        }


        protected override SortOperationDescriptor CreateOperation()
        {
            var operation = new SortOperation(Definition.Property);

            var typeReference = new ClrTypeReference(
                typeof(SortOperationKindType),
                TypeContext.Input);

            return SortOperationDescriptor.New(
                Context,
                Definition.Name,
                typeReference,
                operation);
        }
    }
}
