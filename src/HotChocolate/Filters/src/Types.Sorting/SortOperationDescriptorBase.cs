using System;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types.Sorting;

[Obsolete("Use HotChocolate.Data.")]
public abstract class SortOperationDescriptorBase
    : ArgumentDescriptorBase<SortOperationDefintion>
{
    protected SortOperationDescriptorBase(
        IDescriptorContext context,
        string name,
        TypeReference type,
        SortOperation operation)
        : base(context)
    {
        Definition.Name = name;
        Definition.Type = type ?? throw new ArgumentNullException(nameof(type));
        Definition.Operation = operation ?? throw new ArgumentNullException(nameof(operation));
    }

    protected internal override SortOperationDefintion Definition { get; protected set; } =
        new();

    protected void Name(string value)
    {
        Definition.Name = value;
    }
}
