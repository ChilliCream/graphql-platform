using System;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types.Filters
{
    public class ComparableFilterOperationDescriptor
        : FilterOperationDescriptorBase
        , IComparableFilterOperationDescriptor
    {
        private readonly ComparableFilterFieldDescriptor _descriptor;

        protected ComparableFilterOperationDescriptor(
            IDescriptorContext context,
            ComparableFilterFieldDescriptor descriptor,
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

        public IComparableFilterFieldDescriptor And() => _descriptor;

        public new IComparableFilterOperationDescriptor Name(NameString value)
        {
            base.Name(value);
            return this;
        }

        public new IComparableFilterOperationDescriptor Description(
            string description)
        {
            base.Description(description);
            return this;
        }

        public new IComparableFilterOperationDescriptor Directive<T>(T directive)
           where T : class
        {
            base.Directive(directive);
            return this;
        }

        public new IComparableFilterOperationDescriptor Directive<T>()
            where T : class, new()
        {
            base.Directive<T>();
            return this;
        }

        public new IComparableFilterOperationDescriptor Directive(
            NameString name,
            params ArgumentNode[] arguments)
        {
            base.Directive(name, arguments);
            return this;
        }

        public static ComparableFilterOperationDescriptor New(
            IDescriptorContext context,
            ComparableFilterFieldDescriptor descriptor,
            NameString name,
            ITypeReference type,
            FilterOperation operation) =>
            new ComparableFilterOperationDescriptor(
                context, descriptor, name, type, operation);
    }
}
