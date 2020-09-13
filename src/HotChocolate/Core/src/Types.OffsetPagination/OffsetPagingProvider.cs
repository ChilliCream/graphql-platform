using HotChocolate.Internal;

namespace HotChocolate.Types.Pagination
{
    public abstract class OffsetPagingProvider
        : IPagingProvider
    {
        public abstract bool CanHandle(IExtendedType source);

        IPagingHandler IPagingProvider.CreateHandler(
            IExtendedType source,
            PagingSettings settings) =>
            CreateHandler(source, settings);

        protected abstract OffsetPagingHandler CreateHandler(
            IExtendedType source,
            PagingSettings settings);
    }
}
