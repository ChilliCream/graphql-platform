using HotChocolate.Types;

namespace HotChocolate.Analyzers.Types.EFCore
{
    public class KeyDirectiveType : DirectiveType<KeyDirective>
    {
        protected override void Configure(IDirectiveTypeDescriptor<KeyDirective> descriptor)
        {
            descriptor
                .Name("key")
                .Location(DirectiveLocation.FieldDefinition);

            descriptor
                .Argument(t => t.Name)
                .Description("The name to use for the primary key in the database schema.")
                .Type<NonNullType<StringType>>();
        }
    }
}
