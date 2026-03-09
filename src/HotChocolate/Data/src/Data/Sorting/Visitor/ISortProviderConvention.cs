using HotChocolate.Types.Descriptors;

namespace HotChocolate.Data.Sorting;

internal interface ISortProviderConvention
{
    internal void Initialize(IConventionContext context, ISortConvention convention);

    internal void Complete(IConventionContext context);
}
