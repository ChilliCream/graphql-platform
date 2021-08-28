using HotChocolate.Types;

namespace HotChocolate.CodeGeneration.Types
{
    public class OperationDirectiveType : DirectiveType<OperationDirective>
    {
        protected override void Configure(IDirectiveTypeDescriptor<OperationDirective> descriptor)
        {
            descriptor
                .Name("operation")
                .Location(DirectiveLocation.Object | DirectiveLocation.Schema);

            descriptor
                .Argument(t => t.Operations)
                .Type<ListType<NonNullType<OperationKindType>>>();
        }
    }
}
