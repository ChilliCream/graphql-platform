using HotChocolate.Types;

namespace HotChocolate.CodeGeneration.Types
{
    public class SortDirectionType : EnumType<SortDirection>
    {
        protected override void Configure(IEnumTypeDescriptor<SortDirection> descriptor)
        {
            descriptor.Name("_SortDirection");
        }
    }
}
