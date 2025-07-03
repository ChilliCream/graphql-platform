namespace HotChocolate.AspNetCore.Parsers;

internal sealed class HttpMultipartRequest(string operations, IDictionary<string, IFile> fileMap)
{
    public string Operations { get; set; } = operations;

    public IDictionary<string, IFile> FileMap { get; set; } = fileMap;
}
