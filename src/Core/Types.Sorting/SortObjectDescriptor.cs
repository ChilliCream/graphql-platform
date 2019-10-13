using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types.Sorting
{
    public class SortObjectFieldDescriptor
        : SortFieldDescriptorBase
        , ISortObjectFieldDescriptor
    {
        private readonly Type _type;

        public SortObjectFieldDescriptor(
            IDescriptorContext context,
            PropertyInfo property,
            Type type)
            : base(context, property)
        {
            _type = type;
        }


        public ISortObjectFieldDescriptor AllowSort()
        {
            SortOperationDescriptor field = CreateOperation();
            SortOperations.Add(field);
            return this;
        }

        public new ISortObjectFieldDescriptor Name(NameString value)
        {
            base.Name(value);
            return this;
        }

        public new ISortObjectFieldDescriptor Ignore()
        {
            base.Ignore();
            return this;
        }

        protected override SortOperationDescriptor CreateOperation()
        {
            var operation = new SortOperation(
                Definition.Property);

            var typeReference = new ClrTypeReference(
                typeof(SortInputType<>).MakeGenericType(_type),
                TypeContext.Input);

            return SortOperationDescriptor.New(
                Context,
                Definition.Name,
                typeReference,
                operation);
        }
    }
}
