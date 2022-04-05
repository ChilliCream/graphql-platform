using System;
using System.Collections.Generic;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

#nullable enable

namespace HotChocolate.Configuration;

internal sealed partial class RegisteredType : IHasRuntimeType
{
    private readonly TypeRegistry _typeRegistry;
    private readonly TypeLookup _typeLookup;
    private List<TypeDependency>? _conditionals;

    public RegisteredType(
        TypeSystemObjectBase type,
        bool isInferred,
        TypeRegistry typeRegistry,
        TypeLookup typeLookup,
        IDescriptorContext descriptorContext,
        ITypeInterceptor typeInterceptor,
        string? scope)
    {
        Type = type;
        _typeRegistry = typeRegistry;
        _typeLookup = typeLookup;
        IsInferred = isInferred;
        DescriptorContext = descriptorContext;
        TypeInterceptor = typeInterceptor;
        IsExtension = Type is INamedTypeExtensionMerger;
        IsSchema = Type is ISchema;
        Scope = scope;

        if (type is INamedType nt)
        {
            IsNamedType = true;
            IsIntrospectionType = nt.IsIntrospectionType();
            Kind = nt.Kind;
        }
        else if (type is DirectiveType)
        {
            IsDirectiveType = true;
            Kind = TypeKind.Directive;
        }
    }

    public TypeSystemObjectBase Type { get; }

    public TypeKind? Kind { get; }

    public Type RuntimeType
    {
        get
        {
            return Type is IHasRuntimeType hasClrType
                ? hasClrType.RuntimeType
                : typeof(object);
        }
    }

    public List<ITypeReference> References { get; } = new();

    public List<TypeDependency> Dependencies { get; } = new();

    public List<TypeDependency> Conditionals => _conditionals ??= new();

    public bool IsInferred { get; }

    public bool IsExtension { get; }

    public bool IsNamedType { get; }

    public bool IsDirectiveType { get; }

    public bool IsIntrospectionType { get; }

    public bool IsSchema { get; }

    public bool IsType => IsNamedType;

    public bool IsDirective => IsDirectiveType;

    public List<ISchemaError> Errors => _errors ??= new();

    public void ClearConditionals()
    {
        if (_conditionals is { Count: > 0 })
        {
            _conditionals.Clear();
        }
    }

    public override string? ToString()
    {
        if (IsSchema)
        {
            return "Schema";
        }

        if (Type is IHasName { Name: { IsEmpty: false } } hasName)
        {
            return IsDirective ? $"@{hasName.Name}" : hasName.Name;
        }

        return Type.ToString();
    }
}
