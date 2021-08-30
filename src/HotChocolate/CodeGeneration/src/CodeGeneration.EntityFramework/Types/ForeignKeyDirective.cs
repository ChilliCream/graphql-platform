using HotChocolate.Types;

namespace HotChocolate.CodeGeneration.EntityFramework.Types
{
    public class ForeignKeyDirective
    {
        public string To { get; set; } = default!;

        public string? Name { get; set; }
    }

    public class ForeignKeyDirectiveType : DirectiveType<ForeignKeyDirective>
    {
        protected override void Configure(IDirectiveTypeDescriptor<ForeignKeyDirective> descriptor)
        {
            descriptor
                .Name("foreignKey")
                .Location(DirectiveLocation.FieldDefinition);

            descriptor
                .Argument(t => t.To)
                .Description(
                    "The name of the navigating field this foreign key field enables." +
                    "If the field doesn't exist on the type, it will be a 'shadow field'.")
                .Type<NonNullType<StringType>>();

            descriptor
                .Argument(t => t.Name)
                .Description(
                    "The name to use for the primary key in the database schema. " +
                    "If none is provided, FK_ForeignKeyTable__ForeignKeyColumn__PrimaryKeyTable__PrimaryKeyColumn will be used.")
                .Type<StringType>();
        }
    }
}
