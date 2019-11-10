using System;
using System.Reflection;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types.Sorting
{
    public class SortObjectOperationDescriptor<TObject>
        : SortObjectOperationDescriptor
        , ISortObjectOperationDescriptor<TObject>
    {
        protected SortObjectOperationDescriptor(
            IDescriptorContext context,
            NameString name,
            ITypeReference type,
            SortOperation operation)
            : base(context, name, type, operation)
        {
        }

        public new ISortObjectOperationDescriptor<TObject> Name(NameString value)
        {
            base.Name(value);
            return this;
        }

        public new ISortObjectOperationDescriptor<TObject> Ignore()
        {
            base.Ignore();
            return this;
        }

        public new ISortObjectOperationDescriptor<TObject> Description(string value)
        {
            base.Description(value);
            return this;
        }

        public new ISortObjectOperationDescriptor<TObject> Directive<T>(T directiveInstance)
            where T : class
        {
            base.Directive(directiveInstance);
            return this;
        }

        public new ISortObjectOperationDescriptor<TObject> Directive<T>()
            where T : class, new()
        {
            base.Directive<T>();
            return this;
        }

        public new ISortObjectOperationDescriptor<TObject> Directive(
            NameString name,
            params ArgumentNode[] arguments)
        {
            base.Directive(name, arguments);
            return this;
        }

        public ISortObjectOperationDescriptor<TObject> Type(
            Action<ISortInputTypeDescriptor<TObject>> descriptor)
        {
            var type = new SortInputType<TObject>(descriptor);
            base.Type(type);
            return this;
        }

        public new ISortObjectOperationDescriptor<TObject> Type<TFilter>()
            where TFilter : SortInputType<TObject>
        {
            base.Type<TFilter>();
            return this;
        }

        public new static SortObjectOperationDescriptor<TObject> New(
            IDescriptorContext context,
            NameString name,
            ITypeReference type,
            SortOperation operation) =>
            new SortObjectOperationDescriptor<TObject>(context, name, type, operation);

        public new static SortObjectOperationDescriptor<TObject> CreateOperation(
            PropertyInfo property,
            IDescriptorContext context)
        {
            var operation = new SortOperation(property, true);
            var name = context.Naming.GetMemberName(property, MemberKind.InputObjectField);
            var typeReference = new ClrTypeReference(
                typeof(SortInputType<>).MakeGenericType(typeof(TObject)),
                TypeContext.Input);

            return SortObjectOperationDescriptor<TObject>.New(
                context,
                name,
                typeReference,
                operation);
        }
    }
}
