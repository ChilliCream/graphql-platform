namespace HotChocolate.AspNetCore
{
    public interface IHttpPostMiddlewareOptions
        : IPathOptionAccessor
        , IParserOptionsAccessor
    {
        int MaxRequestSize { get; }
    }
}
