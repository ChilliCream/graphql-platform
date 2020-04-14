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
            SortOperation operation,
            Conventions.ISortingConvention convention)
            : base(context, name, type, operation, convention)
        {
            Definition.Ignore = true;
        }

        public static IgnoredSortingFieldDescriptor New(
            IDescriptorContext context,
            NameString name,
            ITypeReference type,
            SortOperation operation,
            Conventions.ISortingConvention convention) =>
            new IgnoredSortingFieldDescriptor(context, name, type, operation, convention);

        public static IgnoredSortingFieldDescriptor CreateOperation(
            PropertyInfo property,
            IDescriptorContext context,
            Conventions.ISortingConvention convention)
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
