using StrawberryShake;
using StrawberryShake.Persistence.SQLite;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class SQLiteClientBuilderExtensions
    {
        public static IClientBuilder<T> AddSQLitePersistence<T>(
            this IClientBuilder<T> builder,
            string connectionString)
            where T : IStoreAccessor
        {
            builder.Services.AddSingleton(
                sp => new SQLitePersistence(
                    sp.GetRequiredService<T>(),
                    connectionString));

            return builder;
        }
    }
}
