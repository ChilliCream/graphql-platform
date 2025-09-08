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
    private int? _sharedMaxAge;
    private CacheControlScope? _scope;
    private bool? _inheritMaxAge;
    private string[]? _vary;

    public CacheControlAttribute()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CacheControlAttribute"/> class with the specified maximum age.
    /// </summary>
    /// <param name="maxAge">
    /// The maximum time, in seconds, the resource can be cached.
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
                objectField.CacheControl(_maxAge, _scope, _inheritMaxAge, _sharedMaxAge, _vary);
                break;
            case IObjectTypeDescriptor objectType:
                objectType.CacheControl(_maxAge, _scope, _sharedMaxAge, _vary);
                break;
            case IInterfaceTypeDescriptor interfaceType:
                interfaceType.CacheControl(_maxAge, _scope, _sharedMaxAge, _vary);
                break;
            case IUnionTypeDescriptor unionType:
                unionType.CacheControl(_maxAge, _scope, _sharedMaxAge, _vary);
                break;
        }
    }

    /// <summary>
    /// The maximum time, in seconds, this resource can be cached.
    /// </summary>
    public int MaxAge { get => _maxAge ?? CacheControlDefaults.MaxAge; set => _maxAge = value; }

    /// <summary>
    /// The maximum time, in seconds, this resource can be cached on CDNs and other shared caches.
    /// If not set, the value of <c>MaxAge</c> is used for shared caches too.
    /// </summary>
    public int SharedMaxAge { get => _sharedMaxAge ?? 0; set => _sharedMaxAge = value; }

    /// <summary>
    /// The scope of this resource.
    /// </summary>
    public CacheControlScope Scope
    {
        get => _scope ?? CacheControlDefaults.Scope;
        set => _scope = value;
    }

    /// <summary>
    /// Whether this resource should inherit the <c>MaxAge</c> and <c>SharedMaxAge</c>
    /// of its parent.
    /// </summary>
    public bool InheritMaxAge
    {
        get => _inheritMaxAge ?? false;
        set => _inheritMaxAge = value;
    }

    /// <summary>
    /// List of headers that might affect the value of this resource. Typically, these headers becomes part
    /// of the cache key.
    /// </summary>
    public string[]? Vary
    {
        get => _vary ?? [];
        set => _vary = value;
    }
}
