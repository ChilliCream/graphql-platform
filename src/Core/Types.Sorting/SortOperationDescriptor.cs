using System;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types.Sorting
{
    public class SortOperationDescriptor
        : ArgumentDescriptorBase<SortOperationDefintion>
    {

        protected SortOperationDescriptor(
            IDescriptorContext context,
            NameString name,
            ITypeReference type,
            SortOperation operation)
            : base(context)
        {
            Definition.Name = name.EnsureNotEmpty(nameof(name));
            Definition.Type = type
                ?? throw new ArgumentNullException(nameof(type));
            Definition.Operation = operation
                ?? throw new ArgumentNullException(nameof(operation));
        }

        protected sealed override SortOperationDefintion Definition { get; } =
            new SortOperationDefintion();

        protected override void OnCreateDefinition(
            SortOperationDefintion definition)
        {
            if (Definition.Operation.Property is { })
            {
                Context.Inspector.ApplyAttributes(Context, this, Definition.Operation.Property);
            }
            base.OnCreateDefinition(definition);
        }

        public void Name(NameString value)
        {
            Definition.Name = value.EnsureNotEmpty(nameof(value));
        }

        public static SortOperationDescriptor New(
            IDescriptorContext context,
            NameString name,
            ITypeReference type,
            SortOperation operation) =>
            new SortOperationDescriptor(
                context, name, type, operation);
    }
}

