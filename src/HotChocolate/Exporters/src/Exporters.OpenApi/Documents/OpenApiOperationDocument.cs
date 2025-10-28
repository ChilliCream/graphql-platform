using System.Collections.Immutable;
using System.Text;
using HotChocolate.Language;

namespace HotChocolate.Exporters.OpenApi;

public sealed record OpenApiOperationDocument(
    string Id,
    string Name,
    string? Description,
    string HttpMethod,
    OpenApiRoute Route,
    ImmutableArray<OpenApiRouteSegmentParameter> QueryParameters,
    OpenApiRouteSegmentParameter? BodyParameter,
    OperationDefinitionNode OperationDefinition,
    IReadOnlyList<string> FragmentDependencies) : IOpenApiDocument;

// TODO: Move
public sealed record OpenApiRoute(ImmutableArray<IOpenApiRouteSegment> Segments)
{
    public IEnumerable<OpenApiRouteSegmentParameter> Parameters
        => Segments.OfType<OpenApiRouteSegmentParameter>();

    public override string ToString()
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
                sb.Append(parameter);
            }
        }

        return sb.ToString();
    }
}

public interface IOpenApiRouteSegment;

public sealed record OpenApiRouteSegmentLiteral(string Value) : IOpenApiRouteSegment
{
    public override string ToString()
    {
        return Value;
    }
}

public sealed record OpenApiRouteSegmentParameter(
    string Key,
    string Variable,
    ImmutableArray<string>? InputObjectPath)
    : IOpenApiRouteSegment
{
    public override string ToString()
    {
        var sb = new StringBuilder();

        sb.Append('{');

        sb.Append(Key);

        if (Variable != Key)
        {
            sb.Append(':');
            sb.Append('$');
            sb.Append(Variable);

            if (InputObjectPath is not null)
            {
                foreach (var inputObjectPathItem in InputObjectPath)
                {
                    sb.Append('.');
                    sb.Append(inputObjectPathItem);
                }
            }
        }

        sb.Append('}');

        return sb.ToString();
    }
}
