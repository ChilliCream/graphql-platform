using HotChocolate.AspNetCore.Parsers;
using ThrowHelper = HotChocolate.AspNetCore.Utilities.ThrowHelper;

namespace HotChocolate.AspNetCore;

/// <summary>
/// A trie structure for mapping JSON paths to file keys.
/// </summary>
internal sealed class FileMapTrie
{
    private readonly FileMapTrieNode _root = new();

    public FileMapTrieNode Root => _root;

    public void AddPath(VariablePath path, string fileKey)
    {
        var current = _root;

        foreach (var segment in path)
        {
            current = current.GetOrAddNode(segment.ToString());
        }

        current.SetFileKey(fileKey);
    }

    public static FileMapTrie Parse(IDictionary<string, string[]> fileMap)
    {
        var trie = new FileMapTrie();

        foreach (var (fileKey, variablePaths) in fileMap)
        {
            if (variablePaths is null || variablePaths.Length < 1)
            {
                throw ThrowHelper.HttpMultipartMiddleware_NoObjectPath(fileKey);
            }

            foreach (var variablePath in variablePaths)
            {
                trie.AddPath(VariablePath.Parse(variablePath), fileKey);
            }
        }

        return trie;
    }
}
