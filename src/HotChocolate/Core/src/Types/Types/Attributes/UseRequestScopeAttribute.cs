using System;
using System.Reflection;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types;

[AttributeUsage(AttributeTargets.Method)]
public class UseRequestScopeAttribute : ObjectFieldDescriptorAttribute
{
    protected override void OnConfigure(
        IDescriptorContext context, 
        IObjectFieldDescriptor descriptor, 
        MemberInfo member)
        => descriptor.Extend().Definition.DependencyInjectionScope = DependencyInjectionScope.Request;
}