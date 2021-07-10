using HotChocolate.Types;

namespace HotChocolate.Analyzers.Types.EFCore
{
    public class KeyDirective
    {
        public string? Name { get; set; }
    }

    public class KeyDirectiveType : DirectiveType<KeyDirective>
    {
        protected override void Configure(IDirectiveTypeDescriptor<KeyDirective> descriptor)
        {
            descriptor
                .Name("key")
                .Location(DirectiveLocation.FieldDefinition);

            descriptor
                .Argument(t => t.Name)
                .Description(
                    "The name to use for the primary key in the database schema. " +
                    "If none is provided, PK_FieldName will be used.") // TODO: Could we use XML comments?
                .Type<StringType>(); // TODO: Wouldn't this be implied?
        }
    }
}
