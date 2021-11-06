using HotChocolate.Types;

namespace HotChocolate.CodeGeneration.EntityFramework.Types
{
    public class OneToOneDirective
    {
        public string? ForeignKey { get; set; }

        public string? InverseField { get; set; }
    }

    public class OneToOneDirectiveType : DirectiveType<OneToOneDirective>
    {
        protected override void Configure(IDirectiveTypeDescriptor<OneToOneDirective> descriptor)
        {
            descriptor
                .Name("oneToOne")
                .Location(DirectiveLocation.FieldDefinition);

            descriptor
                .Argument(t => t.ForeignKey)
                .Description(
                    "The name of the field to use for the foreign key in this relationship. " +
                    "If none is provided, one will be derived.")
                .Type<StringType>();

            descriptor
                .Argument(t => t.InverseField)
                .Description("The name of the field that navigates back to the current type (if any).")
                .Type<StringType>();
        }
    }
}
