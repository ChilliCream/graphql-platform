using HotChocolate.Types;

namespace HotChocolate.CodeGeneration.EntityFramework.Types
{
    public class ManyToManyDirective
    {
        
        public string JoinTable { get; set; } = default!;

        public string ForeignKey { get; set; } = default!; 

        public string InverseField { get; set; } = default!; 
    }

    public class ManyToManyDirectiveType : DirectiveType<ManyToManyDirective>
    {
        protected override void Configure(IDirectiveTypeDescriptor<ManyToManyDirective> descriptor)
        {
            descriptor
                .Name("manyToMany")
                .Location(DirectiveLocation.FieldDefinition);

            descriptor
               .Argument(t => t.JoinTable)
               .Description("The name of the join table to use in the database schema.")
               .Type<NonNullType<StringType>>();

            descriptor
                .Argument(t => t.ForeignKey)
                .Description("The name of the field to use for the foreign key in this relationship.")
                .Type<NonNullType<StringType>>();

            descriptor
                .Argument(t => t.InverseField)
                .Description("The name of the field that navigates back to the current type.")
                .Type<NonNullType<StringType>>();
        }
    }
}
