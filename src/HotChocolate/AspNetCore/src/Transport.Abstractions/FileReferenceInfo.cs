namespace HotChocolate.Transport.Http;

public sealed class FileReferenceInfo
{
    internal FileReferenceInfo(FileReference file, string name)
    {
        Name = name;
        File = file;
    }

    public string Name { get; }
    
    public FileReference File { get; }
}