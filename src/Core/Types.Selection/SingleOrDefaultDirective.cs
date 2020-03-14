namespace HotChocolate.Types.Selections
{
    public class SingleOrDefaultDirective : DirectiveType
    {
        public const string DIRECTIVE_NAME = "SingleOrDefault";

        protected override void Configure(IDirectiveTypeDescriptor descriptor)
        {
            descriptor.Name(DIRECTIVE_NAME);
            descriptor.Location(DirectiveLocation.FieldDefinition);
        }
    }
}
