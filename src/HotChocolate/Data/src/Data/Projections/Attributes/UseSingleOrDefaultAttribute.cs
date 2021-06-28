using System.Reflection;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Data
{
    /// <summary>
    /// Returns the only element of a sequence, or a default value if the sequence is empty. Applies
    /// the <see cref="UseSingleOrDefaultAttribute"/> to the field.
    /// </summary>
    public sealed class UseSingleOrDefaultAttribute
        : ObjectFieldDescriptorAttribute
    {
        public override void OnConfigure(
            IDescriptorContext context,
            IObjectFieldDescriptor descriptor,
            MemberInfo member)
        {
            descriptor.UseSingleOrDefault();
        }
    }
}
