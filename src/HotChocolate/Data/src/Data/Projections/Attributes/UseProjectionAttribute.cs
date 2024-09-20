using System.Reflection;
using System.Runtime.CompilerServices;
using HotChocolate.Data.Projections;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Data;

/// <summary>
/// Projects the selection set of the request onto the field. Registers a middleware that
/// uses the registered <see cref="ProjectionConvention"/> to apply the projections
/// </summary>
public sealed class UseProjectionAttribute
    : ObjectFieldDescriptorAttribute
{
    public UseProjectionAttribute([CallerLineNumber] int order = 0)
    {
        Order = order;
    }

    /// <summary>
    /// Sets the scope for the convention
    /// </summary>
    /// <value>The name of the scope</value>
    public string? Scope { get; set; }

    /// <inheritdoc />
    protected override void OnConfigure(
        IDescriptorContext context,
        IObjectFieldDescriptor descriptor,
        MemberInfo member)
        => descriptor.UseProjection(Scope);
}
