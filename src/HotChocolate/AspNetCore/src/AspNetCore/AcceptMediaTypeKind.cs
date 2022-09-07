namespace HotChocolate.AspNetCore;

public enum AcceptMediaTypeKind
{
    Unknown = 0,
    All,
    AllApplication,
    AllMultiPart,
    ApplicationGraphQL,
    ApplicationJson,
    MultiPartMixed,
    EventStream
}
