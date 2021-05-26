using HotChocolate.Types;

namespace HotChocolate.Data.Neo4J.Analyzers.Types
{
    public class TypeNameDirectiveType : DirectiveType<TypeNameDirective>
    {
        protected override void Configure(IDirectiveTypeDescriptor<TypeNameDirective> descriptor)
        {
            descriptor
                .Name("typeName")
                .Location(DirectiveLocation.Object);

            descriptor
                .Argument(t => t.Name)
                .Type<NonNullType<StringType>>();

            descriptor
                .Argument(t => t.PluralName)
                .Type<NonNullType<StringType>>();
        }
    }
}
