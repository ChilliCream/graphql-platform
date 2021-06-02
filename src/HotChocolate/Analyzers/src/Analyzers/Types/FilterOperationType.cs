using HotChocolate.Types;

namespace HotChocolate.Analyzers.Types
{
    public class FilterOperationType : EnumType<FilterOperation>
    {
        protected override void Configure(IEnumTypeDescriptor<FilterOperation> descriptor)
        {
            descriptor.Name("_FilterOperation");
        }
    }
}
