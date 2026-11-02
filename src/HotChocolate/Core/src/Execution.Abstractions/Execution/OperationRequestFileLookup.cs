using System.Diagnostics.CodeAnalysis;
using HotChocolate.Types;

namespace HotChocolate.Execution;

internal sealed class OperationRequestFileLookup : IFileLookup
{
    private readonly Dictionary<string, IFile> _files = [];

    public void Add(string name, IFile file)
    {
        _files[name] = file;
    }

    public bool TryGetFile(string name, [NotNullWhen(true)] out IFile? file)
        => _files.TryGetValue(name, out file);
}
