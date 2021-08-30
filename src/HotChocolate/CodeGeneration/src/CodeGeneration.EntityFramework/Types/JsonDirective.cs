using HotChocolate.Types;

namespace HotChocolate.CodeGeneration.EntityFramework.Types
{
    public class JsonDirective
    {

    }

    public class JsonDirectiveType : DirectiveType<JsonDirective>
    {
        public const string NameConst = "json";

        protected override void Configure(IDirectiveTypeDescriptor<JsonDirective> descriptor)
        {
            descriptor
                .Name(NameConst)
                .Location(DirectiveLocation.Object);

        }
    }
}
