using System.Security.Cryptography;
using System.Text;

namespace ChilliCream.Nitro.CommandLine.Services.Memory;

/// <summary>
/// Derives a curated memory's id deterministically from the journal entry id
/// it is promoted from: the same journal id always derives the same curated
/// id, so a concurrent or retried <c>promote</c> of the same entry lands on
/// the same row.
/// </summary>
internal static class MemoryPromotedId
{
    public static string Derive(string journalId)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(journalId));
        return MemoryId.FromHash(hash);
    }
}
