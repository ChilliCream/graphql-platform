using HotChocolate.Types;

namespace HotChocolate.Fusion.Transport.Http;

public readonly record struct FileEntry(string Key, string Path, IFile file);
