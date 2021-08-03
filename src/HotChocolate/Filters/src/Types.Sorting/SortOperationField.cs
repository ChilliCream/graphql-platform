using System;

namespace HotChocolate.Types.Sorting
{
    [Obsolete("Use HotChocolate.Data.")]
    internal sealed class SortOperationField : InputField
    {
        public SortOperationField(
            SortOperationDefintion definition,
            FieldCoordinate coordinate,
            int index)
            : base(definition, coordinate, index)
        {
            Operation = definition.Operation;
        }

        public SortOperation? Operation { get; }
    }
}
