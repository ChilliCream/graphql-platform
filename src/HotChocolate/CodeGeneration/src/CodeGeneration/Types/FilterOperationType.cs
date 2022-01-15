using HotChocolate.Types;

namespace HotChocolate.CodeGeneration.Types
{
    public class FilterOperationType : EnumType<FilterOperation>
    {
        protected override void Configure(IEnumTypeDescriptor<FilterOperation> descriptor)
        {
            descriptor.Name("_FilterOperation");
        }
    }
}
