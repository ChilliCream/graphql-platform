using System.Reflection;
using HotChocolate.Data.Projections;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Data
{
    /// <summary>
    /// Projects the selection set of the request onto the field. Registers a middleware that
    /// uses the registered <see cref="ProjectionConvention"/> to apply the projections
    /// </summary>
    public sealed class UseProjectionAttribute
        : ObjectFieldDescriptorAttribute
    {
        /// <summary>
        /// Sets the scope for the convention
        /// </summary>
        /// <value>The name of the scope</value>
        public string? Scope { get; set; }

        /// <inheritdoc />
        public override void OnConfigure(
            IDescriptorContext context,
            IObjectFieldDescriptor descriptor,
            MemberInfo member)
        {
            descriptor.UseProjection(Scope);
        }
    }
}
