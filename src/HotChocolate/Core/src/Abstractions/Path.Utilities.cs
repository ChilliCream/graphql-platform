using System;
using System.Buffers;
using System.Buffers.Text;
using System.Runtime.CompilerServices;
using Microsoft.Toolkit.HighPerformance.Buffers;

#nullable enable

namespace HotChocolate;

public abstract partial class Path
{
    private const char _separator = '/';
    private const char _leftBrace = '[';
    private const char _rightBrace = ']';
    private const char _format = 'd';

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string CreatePath(string root, string name)
    {
        var size = root.Length + 1 + name.Length;
        char[]? rented = null;
        Span<char> buffer = size < 256
            ? stackalloc char[size]
            : rented = ArrayPool<char>.Shared.Rent(size);

        Span<char> path = buffer;

        if (root.Length > 0)
        {
            root.AsSpan().CopyTo(path);
            path = path.Slice(root.Length);
        }

        path[0] = _separator;
        path = path.Slice(1);
        name.AsSpan().CopyTo(path);
        path = buffer;

        var newPath = StringPool.Shared.GetOrAdd(path.Slice(0, size));

        if (rented is not null)
        {
            ArrayPool<char>.Shared.Return(rented);
        }

        return newPath;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string CreatePath(string root, int index)
    {
        Span<byte> indexBuffer = stackalloc byte[10];
        Utf8Formatter.TryFormat(index, indexBuffer, out var written, _format);

        var size = root.Length + 2 + written;
        char[]? rented = null;
        Span<char> buffer = size < 256
            ? stackalloc char[size]
            : rented = ArrayPool<char>.Shared.Rent(size);

        Span<char> path = buffer;

        root.AsSpan().CopyTo(path);
        path = path.Slice(root.Length);

        path[0] = _leftBrace;
        path= path.Slice(1);

        for (var i = 0; i < written; i++)
        {
            path[i] = (char)indexBuffer[i];
        }

        path[written] = _rightBrace;
        path = buffer;

        var newPath = StringPool.Shared.GetOrAdd(path.Slice(0, size));

        if (rented is not null)
        {
            ArrayPool<char>.Shared.Return(rented);
        }

        return newPath;
    }

    private static NamePathSegment CreateNamePathSegment(string root, NameString name, Path parent)
    {
        if (root is null)
        {
            throw new ArgumentNullException(nameof(root));
        }

        if (parent is null)
        {
            throw new ArgumentNullException(nameof(parent));
        }

        name.EnsureNotEmpty(nameof(name));

        var pathString = CreatePath(root, name);

        _sync.EnterUpgradeableReadLock();

        try
        {
            if (_cache.TryGetValue(pathString, out Path? cachedPath))
            {
                return (NamePathSegment)cachedPath;
            }

            _sync.EnterWriteLock();

            try
            {
                var newPath = new NamePathSegment(parent, name, pathString);
#if NETCOREAPP3_1_OR_GREATER
                _cache.TryAdd(pathString, newPath);
#else
                _cache[pathString] = newPath;
#endif
                return newPath;
            }
            finally
            {
                _sync.ExitWriteLock();
            }
        }
        finally
        {
            _sync.ExitUpgradeableReadLock();
        }
    }
}
