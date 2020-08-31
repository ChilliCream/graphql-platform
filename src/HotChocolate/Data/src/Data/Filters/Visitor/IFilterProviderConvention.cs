using HotChocolate.Types.Descriptors;

namespace HotChocolate.Data.Filters
{
    internal interface IFilterProviderConvention
    {
        void Initialize(IConventionContext context);
    }
}
