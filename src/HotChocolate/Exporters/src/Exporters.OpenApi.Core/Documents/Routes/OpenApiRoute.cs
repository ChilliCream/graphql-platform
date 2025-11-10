using System.Collections.Immutable;
using System.Text;

namespace HotChocolate.Exporters.OpenApi;

public sealed record OpenApiRoute(ImmutableArray<IOpenApiRouteSegment> Segments)
{
    public ImmutableArray<OpenApiRouteSegmentParameter> Parameters { get; }
        = [..Segments.OfType<OpenApiRouteSegmentParameter>()];

    public override string ToString()
    {
        return ToString(false);
    }

    public string ToOpenApiPath()
    {
        return ToString(true);
    }

    private string ToString(bool openApiFormat)
    {
        var sb = new StringBuilder();

        foreach (var segment in Segments)
        {
            sb.Append('/');

            if (segment is OpenApiRouteSegmentLiteral literal)
            {
                sb.Append(literal);
            }
            else if (segment is OpenApiRouteSegmentParameter parameter)
            {
                if (openApiFormat)
                {
                    sb.Append('{');
                    sb.Append(parameter.Key);
                    sb.Append('}');
                }
                else
                {
                    sb.Append(parameter);
                }
            }
        }

        return sb.ToString();
    }
}
