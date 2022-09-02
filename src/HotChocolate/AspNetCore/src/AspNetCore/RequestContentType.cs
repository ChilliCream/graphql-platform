namespace HotChocolate.AspNetCore;

public enum RequestContentType
{
    None,
    Json,
    Form
}

public enum ResponseContentType
{
    NotSupported,
    Json,
    MultiPart,
    GraphQLResponse
}
