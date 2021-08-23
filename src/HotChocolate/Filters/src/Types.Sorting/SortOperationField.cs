using System;

namespace HotChocolate.Types.Sorting
{
    [Obsolete("Use HotChocolate.Data.")]
    internal sealed class SortOperationField : InputField
    {
        public SortOperationField(SortOperationDefintion definition)
            : base(definition, default)
        {
            Operation = definition.Operation;
        }

        public SortOperation? Operation { get; }
    }
}
