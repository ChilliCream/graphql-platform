using StrawberryShake;
using StrawberryShake.Persistence.SQLite;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class SQLiteClientBuilderExtensions
    {
        public static IClientBuilder AddSQLitePersistence(
            this IClientBuilder builder,
            string connectionString)
        {
            builder.Services.AddSingleton(
                sp => new SQLitePersistence(
                    (IStoreAccessor)sp.GetRequiredService(builder.StoreAccessorType),
                    connectionString));

            return builder;
        }
    }
}
