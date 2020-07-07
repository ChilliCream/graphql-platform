using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Data.Filters
{
    public class FilterFieldDefinition : InputFieldDefinition
    {
        public int FieldKind { get; set; }
    }
}
