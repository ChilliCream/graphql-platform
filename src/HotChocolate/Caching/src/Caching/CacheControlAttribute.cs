using System;
using System.Reflection;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Caching;

[AttributeUsage(AttributeTargets.Property
    | AttributeTargets.Method
    | AttributeTargets.Class
    | AttributeTargets.Interface)]
public sealed class CacheControlAttribute : DescriptorAttribute
{
    private int? _maxAge;
    private CacheControlScope? _scope;
    private bool? _inheritMaxAge;

    public CacheControlAttribute()
    {

    }

    public CacheControlAttribute(int maxAge)
    {
        _maxAge = maxAge;
    }

    protected override void TryConfigure(
        IDescriptorContext context,
        IDescriptor descriptor,
        ICustomAttributeProvider element)
    {
        if (element is MemberInfo)
        {
            switch (descriptor)
            {
                case IInterfaceTypeDescriptor interfaceType:
                    interfaceType.CacheControl(_maxAge, _scope, _inheritMaxAge);
                    break;
                case IObjectTypeDescriptor objectType:
                    objectType.CacheControl(_maxAge, _scope, _inheritMaxAge);
                    break;
                case IUnionTypeDescriptor unionType:
                    unionType.CacheControl(_maxAge, _scope, _inheritMaxAge);
                    break;
            }
        }
        else
        {
            switch (descriptor)
            {
                case IInterfaceTypeDescriptor interfaceType:
                    interfaceType.CacheControl(_maxAge, _scope, _inheritMaxAge);
                    break;
                case IObjectTypeDescriptor objectType:
                    objectType.CacheControl(_maxAge, _scope, _inheritMaxAge);
                    break;
                case IUnionTypeDescriptor unionType:
                    unionType.CacheControl(_maxAge, _scope, _inheritMaxAge);
                    break;
            }
        }
    }

    public int MaxAge { get => _maxAge ?? 0; set => _maxAge = value; }

    public CacheControlScope Scope
    {
        get => _scope ?? CacheControlScope.Public;
        set => _scope = value;
    }

    public bool InheritMaxAge
    {
        get => _inheritMaxAge ?? false;
        set => _inheritMaxAge = value;
    }
}