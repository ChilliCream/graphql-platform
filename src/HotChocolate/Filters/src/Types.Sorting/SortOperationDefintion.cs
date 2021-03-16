using System;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types.Sorting
{
    [Obsolete("Use HotChocolate.Data.")]
    public class SortOperationDefintion
        : InputFieldDefinition
    {
        public SortOperation? Operation { get; set; }
    }
}

