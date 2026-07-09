using System.Reflection;
using System.Runtime.CompilerServices;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Data;

/// <summary>
/// Registers the middleware and adds the arguments for sorting
/// </summary>
[AttributeUsage(
    AttributeTargets.Property | AttributeTargets.Method,
    Inherited = true,
    AllowMultiple = true)]
public class UseSortingAttribute : DescriptorAttribute
{
    private static readonly MethodInfo s_genericObject = typeof(SortingObjectFieldDescriptorExtensions)
        .GetMethods(BindingFlags.Public | BindingFlags.Static)
        .Single(m => m.Name.Equals(
                nameof(SortingObjectFieldDescriptorExtensions.UseSorting),
                StringComparison.Ordinal)
            && m.GetGenericArguments().Length == 1
            && m.GetParameters().Length == 2
            && m.GetParameters()[0].ParameterType == typeof(IObjectFieldDescriptor));

    private static readonly MethodInfo s_genericInterface = typeof(SortingObjectFieldDescriptorExtensions)
        .GetMethods(BindingFlags.Public | BindingFlags.Static)
        .Single(m => m.Name.Equals(
                nameof(SortingObjectFieldDescriptorExtensions.UseSorting),
                StringComparison.Ordinal)
            && m.GetGenericArguments().Length == 1
            && m.GetParameters().Length == 2
            && m.GetParameters()[0].ParameterType == typeof(IInterfaceFieldDescriptor));

    public UseSortingAttribute(Type? sortingType = null, [CallerLineNumber] int order = 0)
    {
        Type = sortingType;
        Order = order;
    }

    /// <summary>
    /// Gets or sets the sort type which specifies the sort object structure.
    /// </summary>
    /// <value>The sort type</value>
    public Type? Type { get; set; }

    /// <summary>
    /// Sets the scope for the convention
    /// </summary>
    /// <value>The name of the scope</value>
    public string? Scope { get; set; }

    protected internal override void TryConfigure(
        IDescriptorContext context,
        IDescriptor descriptor,
        ICustomAttributeProvider? attributeProvider)
    {
        if (descriptor is IObjectFieldDescriptor objectFieldDescriptor)
        {
            if (Type is null)
            {
                objectFieldDescriptor.UseSorting(Scope);
            }
            else
            {
                s_genericObject.MakeGenericMethod(Type).Invoke(null, [objectFieldDescriptor, Scope]);
            }
        }

        if (descriptor is IInterfaceFieldDescriptor interfaceFieldDescriptor)
        {
            if (Type is null)
            {
                interfaceFieldDescriptor.UseSorting(Scope);
            }
            else
            {
                s_genericInterface.MakeGenericMethod(Type).Invoke(
                    null,
                    [interfaceFieldDescriptor, Scope]);
            }
        }
    }
}

/// <summary>
/// Specifies the GraphQL type.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Method)]
public sealed class UseSortingAttribute<T> : UseSortingAttribute
{
    public UseSortingAttribute([CallerLineNumber] int order = 0) : base(typeof(T), order)
    {
    }
}
