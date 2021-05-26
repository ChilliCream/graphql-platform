using HotChocolate.Types;

namespace HotChocolate.Data.Neo4J.Analyzers.Types
{
    public class RelationshipDirectionType : EnumType<RelationshipDirection>
    {
        protected override void Configure(IEnumTypeDescriptor<RelationshipDirection> descriptor)
        {
            descriptor.Name("_RelationshipDirection");
        }
    }
}
