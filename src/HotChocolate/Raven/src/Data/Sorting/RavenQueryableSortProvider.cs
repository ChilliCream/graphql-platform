using HotChocolate.Data.Sorting;
using HotChocolate.Data.Sorting.Expressions;
using Raven.Client.Documents.Linq;

namespace HotChocolate.Data.Raven.Sorting;

public class RavenQueryableSortProvider : QueryableSortProvider
{
    public RavenQueryableSortProvider(
        Action<ISortProviderDescriptor<QueryableSortContext>> configure) : base(configure)
    {
    }
}
