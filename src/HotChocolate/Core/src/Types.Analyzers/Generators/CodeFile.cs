using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;
using HotChocolate.Types.Analyzers.Helpers;

namespace HotChocolate.Types.Analyzers.Generators;

public sealed class CodeFile : IDisposable
{
    private StringBuilder? _sb;
    private CodeWriter? _codeWriter;
    private bool _disposed;

    private CodeFile(string @namespace, string typeName)
    {
        FullName = CreateFileName(@namespace, typeName);
        _sb = PooledObjects.GetStringBuilder();
    }

    public string FullName { get; }

    public CodeWriter Writer
    {
        get
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(CodeFile));
            }

            _sb ??= PooledObjects.GetStringBuilder();
            return _codeWriter ??= new CodeWriter(_sb!);
        }
    }

    public override string ToString()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(CodeFile));
        }

        if (_sb is null)
        {
            return string.Empty;
        }

        return _sb.ToString();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        if (_sb is not null)
        {
            _sb!.Clear();
            PooledObjects.Return(_sb);
        }
        _codeWriter = null;
        _sb = null;
        _disposed = true;
    }

    public static CodeFile Create(string @namespace, string typeName)
    {
        return new CodeFile(@namespace, typeName);
    }

    static string CreateFileName(string @namespace, string typeName)
    {
#if NET8_0_OR_GREATER
        Span<byte> hash = stackalloc byte[64];
        var bytes = Encoding.UTF8.GetBytes(@namespace);
        MD5.HashData(bytes, hash);
        Base64.EncodeToUtf8InPlace(hash, 16, out var written);
        hash = hash[..written];

        for (var i = 0; i < hash.Length; i++)
        {
            if (hash[i] == (byte)'+')
            {
                hash[i] = (byte)'-';
            }
            else if (hash[i] == (byte)'/')
            {
                hash[i] = (byte)'_';
            }
            else if (hash[i] == (byte)'=')
            {
                hash = hash[..i];
                break;
            }
        }

        return $"{typeName}.{Encoding.UTF8.GetString(hash)}.hc.g.cs";
#else
        var bytes = Encoding.UTF8.GetBytes(@namespace);
        var md5 = MD5.Create();
        var hash = md5.ComputeHash(bytes);
        var hashString = Convert.ToBase64String(hash, Base64FormattingOptions.None);
        hashString = hashString.Replace("+", "-").Replace("/", "_").TrimEnd('=');
        return $"{typeName}.{hashString}.hc.g.cs";
#endif
    }
}
