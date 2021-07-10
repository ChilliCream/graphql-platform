using HotChocolate.Types;

namespace HotChocolate.Analyzers.Types.EFCore
{
    public class KeyDirective : IEntityFrameworkDirective
    {
        public string? Name { get; set; }

        public object AsConfiguration()
        {
            return null!;
        }
    }

    public class KeyDirectiveType : DirectiveType<KeyDirective>
    {
        protected override void Configure(IDirectiveTypeDescriptor<KeyDirective> descriptor)
        {
            descriptor
                .Name("key") // TODO: Wouldn't this be implied?
                .Location(DirectiveLocation.FieldDefinition);

            descriptor
                .Argument(t => t.Name)
                .Description(
                    "The name to use for the primary key in the database schema. " +
                    "If none is provided, PK_TableName will be used.") // TODO: Could we use XML comments for descs?
                .Type<StringType>(); // TODO: Wouldn't this be implied?
        }
    }
}
