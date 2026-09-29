using System.Data.Common;
using System.Security.Cryptography;
using Dapper;
using Microsoft.Data.Sqlite;

namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

internal static class AgentActorAllocator
{
    internal static IReadOnlyList<string> BaseActors => AgentNamePool.Names;

    public static async Task<string> AllocateAsync(
        SqliteConnection connection,
        DbTransaction transaction)
    {
        // A name is unavailable once it has ever been minted: agent rows, including
        // tombstones whose deleted_at is set.
        var occupied = (await connection.QueryAsync<string>(
                "SELECT name FROM agents",
                transaction: transaction))
            .ToHashSet(StringComparer.Ordinal);
        var names = AgentNamePool.Names;
        var order = Enumerable.Range(0, names.Count).ToArray();

        for (var i = order.Length - 1; i > 0; i--)
        {
            var swap = RandomNumberGenerator.GetInt32(i + 1);
            (order[i], order[swap]) = (order[swap], order[i]);
        }

        foreach (var index in order)
        {
            if (!occupied.Contains(names[index]))
            {
                return names[index];
            }
        }

        // The pool is exhausted: every name is already in use, so append a numeric suffix to a
        // shuffled pick, starting at 2, until one is free.
        for (var suffix = 2; ; suffix++)
        {
            foreach (var index in order)
            {
                var actor = $"{names[index]}-{suffix}";

                if (!occupied.Contains(actor))
                {
                    return actor;
                }
            }
        }
    }
}
