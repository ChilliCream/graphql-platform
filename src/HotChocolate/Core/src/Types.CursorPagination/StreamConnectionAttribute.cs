using System.Reflection;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Configurations;

namespace HotChocolate.Types.Pagination;

/// <summary>
/// Marks the edges and nodes fields of a stream connection as the streamable consumers
/// of the connection item source.
/// </summary>
internal sealed class StreamConnectionAttribute : ObjectTypeDescriptorAttribute
{
    private const string EdgesFieldMember =
        nameof(StreamConnection<object, IEdge<object>, IPageInfo>.Edges);

    private const string NodesFieldMember =
        nameof(StreamConnection<object, IEdge<object>, IPageInfo>.Nodes);

    protected override void OnConfigure(
        IDescriptorContext context,
        IObjectTypeDescriptor descriptor,
        Type? type)
        => descriptor.Extend().OnBeforeCreate(static (_, configuration) => Configure(configuration));

    private static void Configure(ObjectTypeConfiguration configuration)
    {
        foreach (var field in configuration.Fields)
        {
            switch (GetStreamConnectionMemberName(field))
            {
                case EdgesFieldMember:
                    field.HasStreamResult = true;
                    field.SetConnectionEdgesFieldFlags();
                    break;

                case NodesFieldMember:
                    field.HasStreamResult = true;
                    field.SetConnectionNodesFieldFlags();
                    break;
            }
        }
    }

    private static string? GetStreamConnectionMemberName(ObjectFieldConfiguration field)
    {
        if ((field.Member ?? field.ResolverMember) is not PropertyInfo property)
        {
            return null;
        }

        // only a property that resolves to the base declaration is the connection item source.
        var declaringType = property.GetMethod?.GetBaseDefinition().DeclaringType;

        if (declaringType?.IsGenericType is not true
            || declaringType.GetGenericTypeDefinition() != typeof(StreamConnection<,,>))
        {
            return null;
        }

        return property.Name;
    }
}
