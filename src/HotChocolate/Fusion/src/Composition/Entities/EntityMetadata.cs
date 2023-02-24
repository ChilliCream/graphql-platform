using System.Text;

namespace HotChocolate.Fusion.Composition;

public sealed class EntityMetadata
{
    public List<EntityResolver> EntityResolvers { get; } = new();

    public override string ToString()
    {
        var sb = new StringBuilder();

        foreach (var resolver in EntityResolvers)
        {
            if (sb.Length > 0)
            {
                sb.AppendLine();
            }

            sb.AppendLine(resolver.ToString());
        }

        return sb.ToString();
    }
}
