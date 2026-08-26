using System.Data.Common;
using System.Security.Cryptography;
using Dapper;
using Microsoft.Data.Sqlite;

namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

internal static class AgentActorAllocator
{
    private static readonly string[] s_baseActors =
    [
        "alex", "jamie", "sam", "max", "leo", "maya", "nina", "theo", "eli", "luca",
        "mia", "nora", "ben", "adam", "noah", "lena", "eva", "zoe", "ryan", "jack",
        "anna", "emma", "sara", "dani", "chris", "robin", "taylor", "jordan", "casey", "morgan",
        "riley", "jesse", "quinn", "avery", "logan", "dylan", "owen", "clara", "lucy", "sophie",
        "hugo", "felix", "oscar", "henry", "louis", "ella", "grace", "kate", "tom", "will"
    ];

    internal static IReadOnlyList<string> BaseActors => s_baseActors;

    public static async Task<string> AllocateAsync(
        SqliteConnection connection,
        DbTransaction transaction)
    {
        var occupied = (await connection.QueryAsync<string>(
                "SELECT actor FROM agent_session_identities",
                transaction: transaction))
            .ToHashSet(StringComparer.Ordinal);
        var order = Enumerable.Range(0, s_baseActors.Length).ToArray();

        for (var i = order.Length - 1; i > 0; i--)
        {
            var swap = RandomNumberGenerator.GetInt32(i + 1);
            (order[i], order[swap]) = (order[swap], order[i]);
        }

        for (var suffix = 0; ; suffix++)
        {
            foreach (var index in order)
            {
                var actor = suffix == 0 ? s_baseActors[index] : $"{s_baseActors[index]}-{suffix}";

                if (!occupied.Contains(actor))
                {
                    return actor;
                }
            }
        }
    }
}
