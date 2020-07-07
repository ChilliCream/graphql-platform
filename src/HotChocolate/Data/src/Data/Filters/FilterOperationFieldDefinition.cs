using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Data.Filters
{
    public class FilterOperationFieldDefinition : InputFieldDefinition
    {
        public int Operation { get; set; }

        public int FieldKind { get; set; }
    }
}
