using HotChocolate.Types.Descriptors;

namespace HotChocolate.Data.Projections
{
    internal interface IProjectionProviderConvention
    {
        internal void Initialize(IConventionContext context);

        internal void OnComplete(IConventionContext context);
    }
}
