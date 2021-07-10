using HotChocolate.Types;

namespace HotChocolate.Analyzers.Types.EFCore
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
