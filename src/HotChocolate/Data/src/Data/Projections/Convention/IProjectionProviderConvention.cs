using HotChocolate.Types.Descriptors;

namespace HotChocolate.Data.Projections
{
    internal interface IProjectionProviderConvention
    {
        void Initialize(IConventionContext context);
    }
}
