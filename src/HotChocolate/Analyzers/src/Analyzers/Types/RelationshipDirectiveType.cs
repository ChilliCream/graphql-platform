using HotChocolate.Types;

namespace HotChocolate.Data.Neo4J.Analyzers.Types
{
    public class RelationshipDirectiveType : DirectiveType<RelationshipDirective>
    {
        protected override void Configure(IDirectiveTypeDescriptor<RelationshipDirective> descriptor)
        {
            descriptor
                .Name("relationship")
                .Location(DirectiveLocation.FieldDefinition);

            descriptor
                .Argument(t => t.Direction)
                .Type<NonNullType<RelationshipDirectionType>>();
        }
    }
}
