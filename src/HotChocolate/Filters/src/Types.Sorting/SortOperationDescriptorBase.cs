using System;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types.Sorting
{
    public abstract class SortOperationDescriptorBase
        : ArgumentDescriptorBase<SortOperationDefintion>
    {
        protected SortOperationDescriptorBase(
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

        protected internal override SortOperationDefintion Definition { get; protected set; } =
            new SortOperationDefintion();

        protected void Name(NameString value)
        {
            Definition.Name = value.EnsureNotEmpty(nameof(value));
        }
    }
}
