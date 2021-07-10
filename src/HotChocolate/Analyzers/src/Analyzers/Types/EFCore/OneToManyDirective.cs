using HotChocolate.Types;

namespace HotChocolate.Analyzers.Types.EFCore
{
    public class OneToManyDirective
    {
        public string ForeignKey { get; set; } = default!;

        public string? InverseField { get; set; }
    }

    public class OneToManyDirectiveType : DirectiveType<OneToManyDirective>
    {
        protected override void Configure(IDirectiveTypeDescriptor<OneToManyDirective> descriptor)
        {
            descriptor
                .Name("oneToMany")
                .Location(DirectiveLocation.FieldDefinition);

            descriptor
                .Argument(t => t.ForeignKey)
                .Description("The name of the field to use for the foreign key in this relationship.")
                .Type<NonNullType<StringType>>();

            descriptor
                .Argument(t => t.InverseField)
                .Description("The name of the field that navigates back to the current type (if any).")
                .Type<StringType>();
        }
    }
}
