using HotChocolate.Types;

namespace HotChocolate.CodeGeneration.EntityFramework.Types
{
    public class IndexDirective
    {
        public string Name { get; set; } = default!;
    }

    public class IndexDirectiveType : DirectiveType<IndexDirective>
    {
        protected override void Configure(IDirectiveTypeDescriptor<IndexDirective> descriptor)
        {
            descriptor
                .Name("index")
                .Location(DirectiveLocation.FieldDefinition);

            descriptor
                .Argument(t => t.Name)
                .Description(
                    "The name to use for the index in the database schema. " +
                    "If none is provided, IX_TableName_ColumnName will be used.")
                .Type<StringType>();
        }
    }
}
