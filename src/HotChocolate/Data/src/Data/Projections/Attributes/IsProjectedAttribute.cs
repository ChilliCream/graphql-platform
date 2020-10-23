using System;
using System.Reflection;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Data
{
    [AttributeUsage(
        AttributeTargets.Property | AttributeTargets.Method,
        Inherited = true,
        AllowMultiple = true)]
    public class IsProjectedAttribute : ObjectFieldDescriptorAttribute
    {
        private readonly bool _isProjected = true;

        public IsProjectedAttribute(bool isProjected)
        {
            _isProjected = isProjected;
        }

        public IsProjectedAttribute()
        {
        }

        public override void OnConfigure(
            IDescriptorContext context,
            IObjectFieldDescriptor descriptor,
            MemberInfo member)
        {
            descriptor.IsProjected(_isProjected);
        }
    }
}
