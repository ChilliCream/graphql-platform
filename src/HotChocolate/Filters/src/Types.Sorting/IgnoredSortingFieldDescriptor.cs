using System.Reflection;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Sorting.Conventions;

namespace HotChocolate.Types.Sorting
{
    internal class IgnoredSortingFieldDescriptor
       : SortOperationDescriptorBase
    {
        protected IgnoredSortingFieldDescriptor(
            IDescriptorContext context,
            NameString name,
            ITypeReference type,
            SortOperation operation,
            ISortingConvention convention)
            : base(context, name, type, operation, convention)
        {
            Definition.Ignore = true;
        }

        public static IgnoredSortingFieldDescriptor New(
            IDescriptorContext context,
            NameString name,
            ITypeReference type,
            SortOperation operation,
            ISortingConvention convention) =>
                new IgnoredSortingFieldDescriptor(context, name, type, operation, convention);

        public static IgnoredSortingFieldDescriptor CreateOperation(
            PropertyInfo property,
            IDescriptorContext context,
            ISortingConvention convention)
        {
            var operation = new SortOperation(property);
            var typeReference = new ClrTypeReference(
                typeof(SortOperationKindType),
                TypeContext.Input);
            NameString name = context.Naming.GetMemberName(
                property, MemberKind.InputObjectField);

            return IgnoredSortingFieldDescriptor.New(
                context,
                name,
                typeReference,
                operation,
                convention
            );
        }
    }
}
