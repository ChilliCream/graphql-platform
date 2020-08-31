using System.Reflection;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types.Sorting
{
    internal class IgnoredSortingFieldDescriptor
       : SortOperationDescriptorBase
    {
        protected IgnoredSortingFieldDescriptor(
            IDescriptorContext context,
            NameString name,
            ITypeReference type,
            SortOperation operation)
            : base(context, name, type, operation)
        {

            Definition.Ignore = true;
        }

        public static IgnoredSortingFieldDescriptor New(
            IDescriptorContext context,
            NameString name,
            ITypeReference type,
            SortOperation operation) =>
            new IgnoredSortingFieldDescriptor(context, name, type, operation);


        public static IgnoredSortingFieldDescriptor CreateOperation(
            PropertyInfo property,
            IDescriptorContext context)
        {
            var operation = new SortOperation(property);
            var typeReference = context.TypeInspector.GetTypeRef(
                typeof(SortOperationKindType),
                TypeContext.Input);
            NameString name = context.Naming.GetMemberName(
                property, MemberKind.InputObjectField);

            return IgnoredSortingFieldDescriptor.New(
                context,
                name,
                typeReference,
                operation
            );
        }
    }
}
