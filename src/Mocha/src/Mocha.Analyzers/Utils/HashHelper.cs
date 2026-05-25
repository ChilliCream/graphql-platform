using System.Security.Cryptography;
using System.Text;

namespace Mocha.Analyzers.Utils;

/// <summary>
/// Provides non-security hashing utilities for source-generator file name salting.
/// </summary>
internal static class HashHelper
{
#pragma warning disable CA5351 // MD5 is used for non-security hashing (file name salting)
    private static readonly MD5 s_md5 = MD5.Create();
#pragma warning restore CA5351

    /// <summary>
    /// Computes a URL-safe Base64 salt from the given assembly name.
    /// </summary>
    public static string ComputeSalt(string assemblyName)
    {
        byte[] hashBytes;

        lock (s_md5)
        {
            hashBytes = s_md5.ComputeHash(Encoding.UTF8.GetBytes(assemblyName));
        }

        var base64 = Convert.ToBase64String(hashBytes, Base64FormattingOptions.None);

        return base64.Replace("+", "-").Replace("/", "_").TrimEnd('=');
    }
}
