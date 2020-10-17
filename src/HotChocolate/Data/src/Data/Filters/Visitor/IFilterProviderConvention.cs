using HotChocolate.Types.Descriptors;

namespace HotChocolate.Data.Filters
{
    internal interface IFilterProviderConvention
    {
        internal void Initialize(IConventionContext context);

        internal void OnComplete(IConventionContext context);
    }
}
