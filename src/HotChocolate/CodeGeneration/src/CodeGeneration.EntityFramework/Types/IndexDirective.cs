using HotChocolate.Types;

namespace HotChocolate.CodeGeneration.EntityFramework.Types
{
    public class IndexDirective
    {

    }

    public class IndexDirectiveType : DirectiveType<IndexDirective>
    {
        protected override void Configure(IDirectiveTypeDescriptor<IndexDirective> descriptor)
        {
            descriptor
                .Name("index")
                .Location(DirectiveLocation.FieldDefinition);
        }
    }
}
