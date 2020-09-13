using HotChocolate.Internal;

#nullable enable

namespace HotChocolate.Types.Pagination
{
    public interface IPagingProvider
    {
        bool CanHandle(IExtendedType source);

        IPagingHandler CreateHandler(IExtendedType source, PagingSettings settings);
    }
}
