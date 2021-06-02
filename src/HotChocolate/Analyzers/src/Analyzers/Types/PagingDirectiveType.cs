using HotChocolate.Types;

namespace HotChocolate.Analyzers.Types
{
    public class PagingDirectiveType : DirectiveType<PagingDirective>
    {
        protected override void Configure(IDirectiveTypeDescriptor<PagingDirective> descriptor)
        {
            descriptor
                .Name("paging")
                .Location(DirectiveLocation.Object | DirectiveLocation.Schema);

            descriptor
                .Argument(t => t.Kind)
                .Type<PagingKindType>();

            descriptor
                .Argument(t => t.DefaultPageSize)
                .Type<IntType>()
                .DefaultValue(10);

            descriptor
                .Argument(t => t.MaxPageSize)
                .Type<IntType>()
                .DefaultValue(50);

            descriptor
                .Argument(t => t.IncludeTotalCount)
                .Type<BooleanType>()
                .DefaultValue(false);
        }
    }
}
