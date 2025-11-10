using System.Collections.Immutable;
using System.Text;

namespace HotChocolate.Adapters.OpenApi;

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
