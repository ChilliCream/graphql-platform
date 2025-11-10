namespace HotChocolate.Exporters.OpenApi;

public sealed record OpenApiRouteSegmentLiteral(string Value) : IOpenApiRouteSegment
{
    public override string ToString()
    {
        return Value;
    }
}
