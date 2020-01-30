using System;
using System.Reflection;
using HotChocolate.Language;
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

        internal protected override SortOperationDefintion Definition { get; } =
            new SortOperationDefintion();

        protected void Name(NameString value)
        {
            Definition.Name = value.EnsureNotEmpty(nameof(value));
        }
    }
}
