using System;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types.Filters
{
    public class BooleanFilterOperationDescriptor
        : FilterOperationDescriptorBase
        , IBooleanFilterOperationDescriptor
    {
        private readonly BooleanFilterFieldDescriptor _descriptor;

        protected BooleanFilterOperationDescriptor(
            IDescriptorContext context,
            BooleanFilterFieldDescriptor descriptor,
            NameString name,
            ITypeReference type,
            FilterOperation operation)
            : base(context)
        {
            Definition.Name = name.EnsureNotEmpty(nameof(name));
            Definition.Type = type
                ?? throw new ArgumentNullException(nameof(type));
            Definition.Operation = operation
                ?? throw new ArgumentNullException(nameof(operation));
            _descriptor = descriptor
                ?? throw new ArgumentNullException(nameof(descriptor));
        }

        public IBooleanFilterFieldDescriptor And() => _descriptor;

        public new IBooleanFilterOperationDescriptor Name(NameString value)
        {
            base.Name(value);
            return this;
        }

        public new IBooleanFilterOperationDescriptor Description(
            string value)
        {
            base.Description(value);
            return this;
        }

        public new IBooleanFilterOperationDescriptor Directive<T>(
            T directiveInstance)
           where T : class
        {
            base.Directive(directiveInstance);
            return this;
        }

        public new IBooleanFilterOperationDescriptor Directive<T>()
            where T : class, new()
        {
            base.Directive<T>();
            return this;
        }

        public new IBooleanFilterOperationDescriptor Directive(
            NameString name,
            params ArgumentNode[] arguments)
        {
            base.Directive(name, arguments);
            return this;
        }

        public static BooleanFilterOperationDescriptor New(
            IDescriptorContext context,
            BooleanFilterFieldDescriptor descriptor,
            NameString name,
            ITypeReference type,
            FilterOperation operation) =>
            new BooleanFilterOperationDescriptor(
                context, descriptor, name, type, operation);
    }
}
