using System.Reflection;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types.Sorting
{
    public class SortObjectOperationDescriptor
        : SortOperationDescriptor
        , ISortObjectOperationDescriptor
    {
        protected SortObjectOperationDescriptor(
            IDescriptorContext context,
            NameString name,
            ITypeReference type,
            SortOperation operation)
            : base(context, name, type, operation)
        {
        }

        public new ISortObjectOperationDescriptor Name(NameString value)
        {
            base.Name(value);
            return this;
        }

        public new ISortObjectOperationDescriptor Ignore()
        {
            base.Ignore();
            return this;
        }

        public new ISortObjectOperationDescriptor Description(string value)
        {
            base.Description(value);
            return this;
        }

        public new ISortObjectOperationDescriptor Directive<T>(T directiveInstance)
            where T : class
        {
            base.Directive(directiveInstance);
            return this;
        }

        public new ISortObjectOperationDescriptor Directive<T>()
            where T : class, new()
        {
            base.Directive<T>();
            return this;
        }

        public new ISortObjectOperationDescriptor Directive(
            NameString name,
            params ArgumentNode[] arguments)
        {
            base.Directive(name, arguments);
            return this;
        }

        public new static SortObjectOperationDescriptor New(
            IDescriptorContext context,
            NameString name,
            ITypeReference type,
            SortOperation operation) =>
            new SortObjectOperationDescriptor(context, name, type, operation);

        public new static SortObjectOperationDescriptor CreateOperation(
            PropertyInfo property,
            IDescriptorContext context)
        {
            var type = property.PropertyType;
            var operation = new SortOperation(property, true);
            var name = context.Naming.GetMemberName(
               property, MemberKind.InputObjectField);
            var typeReference = new ClrTypeReference(
                typeof(SortInputType<>).MakeGenericType(type),
                TypeContext.Input);

            return SortObjectOperationDescriptor.New(
                context,
                name,
                typeReference,
                operation);
        }
    }
}
