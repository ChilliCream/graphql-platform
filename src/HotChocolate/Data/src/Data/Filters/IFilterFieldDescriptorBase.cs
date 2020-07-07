using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Data.Filters
{
    public interface IFilterFieldDescriptorBase
    {
        InputFieldDefinition CreateFieldDefinition();
    }
}
