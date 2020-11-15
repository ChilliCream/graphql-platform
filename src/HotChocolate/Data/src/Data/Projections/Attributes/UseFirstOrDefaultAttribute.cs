using System.Reflection;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Data
{
    /// <summary>
    /// Returns the first element of the sequence that satisfies a condition or a default value if
    /// no such element is found. Applies the <see cref="UseSingleOrDefaultAttribute"/> to the field
    /// </summary>
    public sealed class UseFirstOrDefaultAttribute
        : ObjectFieldDescriptorAttribute
    {
        public override void OnConfigure(
            IDescriptorContext context,
            IObjectFieldDescriptor descriptor,
            MemberInfo member)
        {
            descriptor.UseFirstOrDefault();
        }
    }
}
