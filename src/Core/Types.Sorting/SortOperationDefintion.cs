using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types.Sorting
{
    public class SortOperationDefintion
        : InputFieldDefinition
    {
        public SortOperation Operation { get; set; }
    }
}

