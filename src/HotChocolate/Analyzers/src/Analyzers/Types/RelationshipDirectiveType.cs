using HotChocolate.Types;

namespace HotChocolate.Analyzers.Types
{
    public class RelationshipDirectiveType : DirectiveType<RelationshipDirective>
    {
        protected override void Configure(IDirectiveTypeDescriptor<RelationshipDirective> descriptor)
        {
            descriptor
                .Name("relationship")
                .Location(DirectiveLocation.FieldDefinition);

            descriptor
                .Argument(t => t.Name)
                .Type<NonNullType<StringType>>();

            descriptor
                .Argument(t => t.Direction)
                .Type<NonNullType<RelationshipDirectionType>>()
                .DefaultValue(RelationshipDirection.Out);
        }
    }
}
