using HotChocolate.Types.Descriptors;

namespace HotChocolate.Data.Sorting
{
    internal interface ISortProviderConvention
    {
        internal void Initialize(IConventionContext context);

        internal void Complete(IConventionContext context);
    }
}
