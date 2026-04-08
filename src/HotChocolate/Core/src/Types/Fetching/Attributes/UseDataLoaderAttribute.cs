using System.Diagnostics.CodeAnalysis;
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

    [UnconditionalSuppressMessage("AOT", "IL3050",
        Justification = "DataLoader types are statically referenced in schema definitions.")]
    [UnconditionalSuppressMessage("AOT", "IL2077",
        Justification = "DataLoader types are statically referenced in schema definitions.")]
    protected override void OnConfigure(
        IDescriptorContext context,
        IObjectFieldDescriptor descriptor,
        MemberInfo? member)
        => descriptor.UseDataLoader(_dataLoaderType);
}
