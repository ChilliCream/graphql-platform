using HotChocolate.Types;
using HotChocolate.StarWars.Models;

namespace HotChocolate.StarWars.Types;

public class ReviewType : ObjectType<Review>
{
    protected override void Configure(IObjectTypeDescriptor<Review> descriptor)
    {
        // we introduce a resolver to the field so that this field cannot be inlined for
        // our query plan tests.
        descriptor
            .Field(t => t.Commentary)
            .Resolve(ctx => Task.FromResult(ctx.Parent<Review>().Commentary));
    }
}
