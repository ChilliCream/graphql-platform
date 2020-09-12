using HotChocolate.Types.Descriptors;

namespace HotChocolate.Data.Sorting
{
    internal interface ISortProviderConvention
    {
        void Initialize(IConventionContext context);
    }
}
