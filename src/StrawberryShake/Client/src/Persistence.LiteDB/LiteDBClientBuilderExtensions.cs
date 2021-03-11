using StrawberryShake;
using StrawberryShake.Persistence.LiteDB;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class LiteDBClientBuilderExtensions
    {
        public static IClientBuilder<T> AddLiteDBPersistence<T>(
            this IClientBuilder<T> builder,
            string fileName)
            where T : IStoreAccessor
        {
            builder.Services.AddSingleton(
                sp => new LiteDBPersistence(
                    sp.GetRequiredService<T>(),
                    fileName));

            return builder;
        }
    }
}
