using System;
using System.IO;

namespace HotChocolate.Transport.Http;

public sealed class FileReference
{
    private readonly Func<Stream> _openRead;

    public FileReference(Func<Stream> openRead, string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(fileName));
        }
        
        _openRead = openRead ?? throw new ArgumentNullException(nameof(openRead));
        FileName = fileName;
    }

    public string FileName { get; }
    
    public Stream OpenRead() => _openRead();
}