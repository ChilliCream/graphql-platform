using System;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types.Filters.String
{
    public class StringFilterOperationDescriptor
        : FilterOperationDescriptorBase
        , IStringFilterOperationDescriptor
    {
        private readonly StringFilterFieldDescriptor _descriptor;

        protected StringFilterOperationDescriptor(
            IDescriptorContext context,
            StringFilterFieldDescriptor descriptor,
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

        public IStringFilterFieldDescriptor And() => _descriptor;

        public new IStringFilterOperationDescriptor Name(NameString value)
        {
            base.Name(value);
            return this;
        }

        public new IStringFilterOperationDescriptor Description(
            string description)
        {
            base.Description(description);
            return this;
        }

        public new IStringFilterOperationDescriptor Directive<T>(T directive)
           where T : class
        {
            base.Directive(directive);
            return this;
        }

        public new IStringFilterOperationDescriptor Directive<T>()
            where T : class, new()
        {
            base.Directive<T>();
            return this;
        }

        public new IStringFilterOperationDescriptor Directive(
            NameString name,
            params ArgumentNode[] arguments)
        {
            base.Directive(name, arguments);
            return this;
        }

        public static StringFilterOperationDescriptor New(
            IDescriptorContext context,
            StringFilterFieldDescriptor descriptor,
            NameString name,
            ITypeReference type,
            FilterOperation operation) =>
            new StringFilterOperationDescriptor(
                context, descriptor, name, type, operation);
    }
}
