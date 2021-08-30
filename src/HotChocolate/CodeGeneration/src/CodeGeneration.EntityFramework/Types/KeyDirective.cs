using HotChocolate.Types;

namespace HotChocolate.CodeGeneration.EntityFramework.Types
{
    public class KeyDirective
    {
        /// <summary>
        /// The name to use for the primary key in the database schema.
        /// If none is provided, PK_TableName will be used.
        /// </summary>
        public string? Name { get; set; }
    }

    public class KeyDirectiveType : DirectiveType<KeyDirective>
    {
        protected override void Configure(IDirectiveTypeDescriptor<KeyDirective> descriptor)
        {
            descriptor
                .Name("key")
                .Description(
                    "Annotates a field as a primary key. " +
                    "If used on multiple fields within the same type, a composite key will be formed.")
                .Location(DirectiveLocation.FieldDefinition);

            descriptor
                .Argument(t => t.Name)
                .Description(
                    "The name to use for the primary key in the database schema. " +
                    "If none is provided, PK_TableName will be used.")
                .Type<StringType>();
        }
    }
}
