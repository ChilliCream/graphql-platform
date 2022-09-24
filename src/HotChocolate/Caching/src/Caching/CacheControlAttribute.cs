using System;
using System.Reflection;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Caching;

/// <summary>
/// Specifies caching rules for the annotated resource.
/// </summary>
[AttributeUsage(AttributeTargets.Property
    | AttributeTargets.Method
    | AttributeTargets.Class
    | AttributeTargets.Interface
    )]
public sealed class CacheControlAttribute : DescriptorAttribute
{
    private int? _maxAge;
    private CacheControlScope? _scope;
    private bool? _inheritMaxAge;

    public CacheControlAttribute()
    {
    }

    /// <param name="maxAge">
    /// The maximum time, in Milliseconds, the resource can be cached.
    /// </param>
    public CacheControlAttribute(int maxAge)
    {
        _maxAge = maxAge;
    }

    protected internal override void TryConfigure(
        IDescriptorContext context,
        IDescriptor descriptor,
        ICustomAttributeProvider element)
    {
        switch (descriptor)
        {
            case IObjectFieldDescriptor objectField:
                objectField.CacheControl(_maxAge, _scope, _inheritMaxAge);
                break;
            case IObjectTypeDescriptor objectType:
                objectType.CacheControl(_maxAge, _scope);
                break;
            case IInterfaceTypeDescriptor interfaceType:
                interfaceType.CacheControl(_maxAge, _scope);
                break;
            case IUnionTypeDescriptor unionType:
                unionType.CacheControl(_maxAge, _scope);
                break;
        }
    }

    /// <summary>
    /// The maximum time, in Milliseconds, this resource can be cached.
    /// </summary>
    public int MaxAge { get => _maxAge ?? 0; set => _maxAge = value; }

    /// <summary>
    /// The scope of this resource.
    /// </summary>
    public CacheControlScope Scope
    {
        get => _scope ?? CacheControlScope.Public;
        set => _scope = value;
    }

    /// <summary>
    /// Whether this resource should inherit the <c>MaxAge</c>
    /// of its parent.
    /// </summary>
    public bool InheritMaxAge
    {
        get => _inheritMaxAge ?? false;
        set => _inheritMaxAge = value;
    }
}
