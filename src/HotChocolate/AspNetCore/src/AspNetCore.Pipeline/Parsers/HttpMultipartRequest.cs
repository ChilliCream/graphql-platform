namespace HotChocolate.AspNetCore.Parsers;

internal sealed record HttpMultipartRequest(
    string Operations,
    IFileLookup Files,
    FileMapTrie FileMap);
