using System.Reflection;
using System.Runtime.CompilerServices;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Data;

/// <summary>
/// Registers the middleware and adds the arguments for sorting
/// </summary>
public class UseSortingAttribute : ObjectFieldDescriptorAttribute
{
    private static readonly MethodInfo _generic = typeof(SortingObjectFieldDescriptorExtensions)
        .GetMethods(BindingFlags.Public | BindingFlags.Static)
        .Single(m => m.Name.Equals(
                nameof(SortingObjectFieldDescriptorExtensions.UseSorting),
                StringComparison.Ordinal)
            && m.GetGenericArguments().Length == 1
            && m.GetParameters().Length == 2
            && m.GetParameters()[0].ParameterType == typeof(IObjectFieldDescriptor));

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

    protected override void OnConfigure(
        IDescriptorContext context,
        IObjectFieldDescriptor descriptor,
        MemberInfo member)
    {
        if (Type is null)
        {
            descriptor.UseSorting(Scope);
        }
        else
        {
            _generic.MakeGenericMethod(Type).Invoke(null, [descriptor, Scope,]);
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
