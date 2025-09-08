using System.Reflection;
using System.Runtime.CompilerServices;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types;

public sealed class UseDataLoaderAttribute : ObjectFieldDescriptorAttribute
{
    private readonly Type _dataLoaderType;

    public UseDataLoaderAttribute(Type dataLoaderType, [CallerLineNumber] int order = 0)
    {
        _dataLoaderType = dataLoaderType;
        Order = order;
    }

    protected override void OnConfigure(
        IDescriptorContext context,
        IObjectFieldDescriptor descriptor,
        MemberInfo member)
    {
        descriptor.UseDataLoader(_dataLoaderType);
    }
}
