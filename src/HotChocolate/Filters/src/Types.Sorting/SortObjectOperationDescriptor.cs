using System;
using System.Reflection;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types.Sorting
{
    public class SortObjectOperationDescriptor
        : SortOperationDescriptorBase
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

        /// <inheritdoc/>
        public new ISortObjectOperationDescriptor Name(NameString value)
        {
            base.Name(value);
            return this;
        }

        /// <inheritdoc/>
        public ISortObjectOperationDescriptor Ignore(bool ignore = true)
        {
            Definition.Ignore = ignore;
            return this;
        }

        /// <inheritdoc/>
        public new ISortObjectOperationDescriptor Description(string value)
        {
            base.Description(value);
            return this;
        }

        /// <inheritdoc/>
        public new ISortObjectOperationDescriptor Directive<T>(T directiveInstance)
            where T : class
        {
            base.Directive(directiveInstance);
            return this;
        }

        /// <inheritdoc/>
        public new ISortObjectOperationDescriptor Directive<T>()
            where T : class, new()
        {
            base.Directive<T>();
            return this;
        }

        /// <inheritdoc/>
        public new ISortObjectOperationDescriptor Directive(
            NameString name,
            params ArgumentNode[] arguments)
        {
            base.Directive(name, arguments);
            return this;
        }

        public static SortObjectOperationDescriptor New(
            IDescriptorContext context,
            NameString name,
            ITypeReference type,
            SortOperation operation) =>
            new SortObjectOperationDescriptor(context, name, type, operation);

        public static SortObjectOperationDescriptor CreateOperation(
            PropertyInfo property,
            IDescriptorContext context)
        {
            Type type = property.PropertyType;
            var operation = new SortOperation(property, true);
            NameString name = context.Naming.GetMemberName(
               property, MemberKind.InputObjectField);
            var typeReference = context.TypeInspector.GetTypeRef(
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
