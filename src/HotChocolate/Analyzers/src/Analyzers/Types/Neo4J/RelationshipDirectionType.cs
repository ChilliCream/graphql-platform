using HotChocolate.Types;

namespace HotChocolate.Analyzers.Types.Neo4J
{
    public class RelationshipDirectionType : EnumType<RelationshipDirection>
    {
        protected override void Configure(IEnumTypeDescriptor<RelationshipDirection> descriptor)
        {
            descriptor.Name("_RelationshipDirection");
        }
    }
}
