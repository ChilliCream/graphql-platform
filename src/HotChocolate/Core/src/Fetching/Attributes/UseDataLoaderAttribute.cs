using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using HotChocolate.Types.Descriptors;

#nullable enable

namespace HotChocolate.Types;

public sealed class UseDataLoaderAttribute : ObjectFieldDescriptorAttribute
{
    private readonly Type _dataLoaderType;

    public UseDataLoaderAttribute(Type dataLoaderType, [CallerLineNumber] int order = 0)
    {
        _dataLoaderType = dataLoaderType;
        Order = order;
    }

    public override void OnConfigure(
        IDescriptorContext context,
        IObjectFieldDescriptor descriptor,
        MemberInfo member)
    {
        descriptor.UseDataloader(_dataLoaderType);
    }
}
