using System.Security.Cryptography;
using System.Text;

namespace ChilliCream.Nitro.CommandLine.Services.Memory;

/// <summary>
/// Derives a curated memory id deterministically from its source journal entry id.
/// </summary>
internal static class MemoryPromotedId
{
    public static string Derive(string journalId)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(journalId));
        return MemoryId.FromHash(hash);
    }
}
