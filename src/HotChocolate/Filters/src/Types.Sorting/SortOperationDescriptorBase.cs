using System;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Sorting.Conventions;

namespace HotChocolate.Types.Sorting
{
    public abstract class SortOperationDescriptorBase
        : ArgumentDescriptorBase<SortOperationDefintion>
    {
        protected SortOperationDescriptorBase(
            IDescriptorContext context,
            NameString name,
            ITypeReference type,
            SortOperation operation,
            ISortingConvention convention)
            : base(context)
        {
            Definition.Name = name.EnsureNotEmpty(nameof(name));
            Definition.Type = type
                ?? throw new ArgumentNullException(nameof(type));
            Definition.Operation = operation
                ?? throw new ArgumentNullException(nameof(operation));
            Convention = convention ??
                throw new ArgumentNullException(nameof(convention));
        }

        internal protected override SortOperationDefintion Definition { get; } =
            new SortOperationDefintion();

        protected ISortingConvention Convention { get; }

        protected void Name(NameString value)
        {
            Definition.Name = value.EnsureNotEmpty(nameof(value));
        }
    }
}
