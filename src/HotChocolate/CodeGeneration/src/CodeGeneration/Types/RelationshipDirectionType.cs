using HotChocolate.Types;

namespace HotChocolate.CodeGeneration.Types
{
    public class RelationshipDirectionType : EnumType<RelationshipDirection>
    {
        protected override void Configure(IEnumTypeDescriptor<RelationshipDirection> descriptor)
        {
            descriptor.Name("_RelationshipDirection");
        }
    }
}
