using GreenDonut.Data;
using HotChocolate.Types.Composite;

namespace HotChocolate.Types.Pagination;

internal sealed class PageCursorType : ObjectType<PageCursor>
{
    protected override void Configure(IObjectTypeDescriptor<PageCursor> descriptor)
    {
        if (descriptor.Extend().Context.Options.ApplyShareableToPageInfo)
        {
            descriptor.Directive(Shareable.Instance);
        }

        descriptor
            .Name("PageCursor")
            .Description("A cursor that points to a specific page.");

        descriptor
            .Field(t => t.Page)
            .Description("The page number.");

        descriptor
            .Field(t => t.Cursor)
            .Description("The cursor.");
    }
}
