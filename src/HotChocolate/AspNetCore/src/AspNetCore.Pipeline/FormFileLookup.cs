using System.Diagnostics.CodeAnalysis;
using HotChocolate.AspNetCore.Parsers;
using Microsoft.AspNetCore.Http;
using ThrowHelper = HotChocolate.AspNetCore.Utilities.ThrowHelper;

namespace HotChocolate.AspNetCore;

internal sealed class FormFileLookup : IFileLookup
{
    private readonly Dictionary<string, IFile> _fileMap;

    private FormFileLookup(Dictionary<string, IFile> fileMap)
    {
        _fileMap = fileMap;
    }

    public bool TryGetFile(string name, [NotNullWhen(true)] out IFile? file)
        => _fileMap.TryGetValue(name, out file);

    public static FormFileLookup Create(
        IDictionary<string, string[]> map,
        IFormFileCollection files)
    {
        var fileMap = new Dictionary<string, IFile>();

        foreach (var fileKey in map.Keys)
        {
            var file = string.IsNullOrEmpty(fileKey) ? null : files.GetFile(fileKey);

            if (file is null)
            {
                throw ThrowHelper.HttpMultipartMiddleware_FileMissing(fileKey);
            }

            fileMap.TryAdd(fileKey, new UploadedFile(file));
        }

        return new FormFileLookup(fileMap);
    }
}
