using System.Security.Cryptography;
using System.Text;

namespace ChilliCream.Nitro.CommandLine.Services.Memory;

/// <summary>
/// Derives a curated memory's id deterministically from the scope and
/// journal entry id it is promoted from: the same (scope, journal id) pair
/// always derives the same curated id, so a concurrent or retried
/// <c>promote</c> of the same journal entry lands on the same curated file.
/// </summary>
internal static class MemoryPromotedId
{
    public static string Derive(string scope, string journalId)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(scope + ":" + journalId));
        return MemoryId.FromHash(hash);
    }
}
