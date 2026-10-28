using System.Reflection;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Relay;

namespace HotChocolate.Types;

public class NodeResolverAttributeProbe
{
    public int Id { get; set; }

    public string? ResolvedBy { get; set; }
}

[ObjectType<NodeResolverAttributeProbe>]
public static partial class NodeResolverAttributeProbeType
{
    [NodeResolver]
    [SetResolvedBy]
    public static NodeResolverAttributeProbe GetNodeResolverAttributeProbeById(int id)
        => new() { Id = id };
}

public sealed class SetResolvedByAttribute : ObjectFieldDescriptorAttribute
{
    protected override void OnConfigure(
        IDescriptorContext context,
        IObjectFieldDescriptor descriptor,
        MemberInfo? member)
    {
        descriptor.Use(next => async ctx =>
        {
            await next(ctx);

            if (ctx.Result is NodeResolverAttributeProbe probe)
            {
                probe.ResolvedBy = member?.Name;
            }
        });
    }
}
