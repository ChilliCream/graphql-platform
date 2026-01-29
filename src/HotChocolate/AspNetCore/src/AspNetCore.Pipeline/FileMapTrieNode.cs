using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.AspNetCore;

internal sealed class FileMapTrieNode
{
    private List<Entry>? _nodes;

    public string? FileKey { get; private set; }

    public FileMapTrieNode GetOrAddNode(string key)
    {
        if (TryGetNode(key, out var node))
        {
            return node;
        }

        var entry = new Entry(key);
        _nodes ??= [];
        _nodes.Add(entry);
        return entry.Node;
    }

    public bool TryGetNode(string key, [NotNullWhen(true)] out FileMapTrieNode? node)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);

        if (_nodes is null)
        {
            node = null;
            return false;
        }

        if (_nodes.Count == 1)
        {
            var current = _nodes[0];

            if (string.Equals(current.Key, key, StringComparison.Ordinal))
            {
                node = current.Node;
                return true;
            }

            node = null;
            return false;
        }

        foreach (var current in _nodes)
        {
            if (string.Equals(current.Key, key, StringComparison.Ordinal))
            {
                node = current.Node;
                return true;
            }
        }

        node = null;
        return false;
    }

    public void SetFileKey(string fileKey)
    {
        ArgumentException.ThrowIfNullOrEmpty(fileKey);

        if (_nodes is not null)
        {
            throw new InvalidOperationException("A filename can only be set on a leaf node.");
        }

        if (!string.IsNullOrEmpty(FileKey))
        {
            throw new InvalidOperationException("A filename can only be set once.");
        }

        FileKey = fileKey;
    }

    private readonly struct Entry(string key)
    {
        public readonly string Key = key;
        public readonly FileMapTrieNode Node = new();
    }
}
