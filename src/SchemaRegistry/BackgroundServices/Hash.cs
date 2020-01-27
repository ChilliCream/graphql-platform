using System;
using System.Security.Cryptography;
using System.Text;

namespace MarshmallowPie.BackgroundServices
{
    internal static class Hash
    {
        public static string ComputeHash(string formattedSourceText)
        {
            using var sha = SHA256.Create();
            return Convert.ToBase64String(sha.ComputeHash(
                Encoding.UTF8.GetBytes(formattedSourceText)));
        }
    }
}
