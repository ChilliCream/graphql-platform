using System;

namespace HotChocolate.Types.Sorting;

[Obsolete("Use HotChocolate.Data.")]
internal sealed class SortOperationField : InputField
{
    public SortOperationField(SortOperationDefintion definition, int index)
        : base(definition, index)
    {
        Operation = definition.Operation;
    }

    public SortOperation? Operation { get; }
}