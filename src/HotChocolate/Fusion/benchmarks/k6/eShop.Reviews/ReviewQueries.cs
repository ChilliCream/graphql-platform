using HotChocolate.Types;
using HotChocolate.Types.Composite;
using HotChocolate.Types.Relay;

namespace eShop.Reviews;

[QueryType]
public static partial class ReviewQueries
{
    [Lookup]
    public static Review? GetReview([ID] string id)
        => ReviewRepository.GetById(id);
}
