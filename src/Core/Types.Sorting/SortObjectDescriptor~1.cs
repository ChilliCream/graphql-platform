using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types.Sorting
{
    public class SortObjectFieldDescriptor<TObject>
        : SortObjectFieldDescriptor
        , ISortObjectFieldDescriptor<TObject>
    {

        public SortObjectFieldDescriptor(
            IDescriptorContext context,
            PropertyInfo property)
            : base(context, property, typeof(TObject))
        {

        }


        public new ISortObjectFieldDescriptor<TObject> Name(NameString value)
        {
            base.Name(value);
            return this;
        }

        public new ISortObjectFieldDescriptor<TObject> Ignore()
        {
            base.Ignore();
            return this;
        }


        public new ISortObjectFieldDescriptor<TObject> AllowSort()
        {
            SortOperationDescriptor field = CreateOperation();
            SortOperations.Add(field);
            return this;
        }

        public ISortObjectFieldDescriptor<TObject> AllowSort(Action<ISortInputTypeDescriptor<TObject>> descriptor)
        {
            var type = new SortInputType<TObject>(descriptor);
            var typeReference = new SchemaTypeReference(type);
            SortOperationDescriptor field = CreateOperation();
            field.Type(typeReference);
            SortOperations.Add(field);
            return this;
        }

        public ISortObjectFieldDescriptor<TObject> AllowSort<TFilter>() where TFilter : SortInputType<TObject>
        {
            SortOperationDescriptor field = CreateOperation();
            field.Type<TFilter>();
            SortOperations.Add(field);

            return this;
        }
    }
}
