using System.Collections.Immutable;
using System.Text;

namespace HotChocolate.Adapters.OpenApi;

public sealed record OpenApiRouteSegmentParameter(
    string Key,
    string VariableName,
    ImmutableArray<string>? InputObjectPath)
    : IOpenApiRouteSegment
{
    public override string ToString()
    {
        var sb = new StringBuilder();

        sb.Append('{');

        sb.Append(Key);

        if (VariableName != Key)
        {
            sb.Append(':');
            sb.Append('$');
            sb.Append(VariableName);

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
