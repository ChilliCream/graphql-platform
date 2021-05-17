using System.Reflection;
using HotChocolate.Types.Descriptors;

#nullable enable

namespace HotChocolate.Types
{
    /// <summary>
    /// Marks a resolver as parallel executable which will allow the execution engine 
    /// to execute this resolver in parallel with other resolvers.
    /// </summary>
    public sealed class ParallelAttribute : ObjectFieldDescriptorAttribute
    {
        public override void OnConfigure(
            IDescriptorContext context,
            IObjectFieldDescriptor descriptor,
            MemberInfo member)
        {
            descriptor.Serial();
        }
    }
}
