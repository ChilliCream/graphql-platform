using HotChocolate.Types.Descriptors;

namespace HotChocolate.Data.Filters
{
    public interface IFilterProviderInitializationContext : IConventionContext
    {
        IFilterConvention Convention { get; }
    }
}

