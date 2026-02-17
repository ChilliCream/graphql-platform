using System.Text;

namespace HotChocolate.Adapters.OpenApi.Packaging;

public readonly record struct OpenApiEndpointKey(string HttpMethod, string Route)
{
    public override string ToString()
    {
        var sb = new StringBuilder();

        if (!string.IsNullOrEmpty(HttpMethod))
        {
            sb.Append(HttpMethod);
            sb.Append(' ');
        }

        sb.Append(Route);

        return sb.ToString();
    }
}
