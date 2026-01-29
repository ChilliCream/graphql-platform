using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Configurations;

namespace HotChocolate.Configuration;

internal sealed partial class RegisteredType : IRuntimeTypeProvider
{
    private readonly TypeRegistry _typeRegistry;
    private readonly TypeLookup _typeLookup;
    private List<TypeDependency>? _conditionals;

    public RegisteredType(
        TypeSystemObject type,
        bool isInferred,
        TypeRegistry typeRegistry,
        TypeLookup typeLookup,
        IDescriptorContext descriptorContext,
        TypeInterceptor typeInterceptor,
        string? scope)
    {
        Type = type;
        _typeRegistry = typeRegistry;
        _typeLookup = typeLookup;
        IsInferred = isInferred;
        DescriptorContext = descriptorContext;
        TypeInterceptor = typeInterceptor;
        IsExtension = Type is ITypeDefinitionExtension;
        IsSchema = Type is Schema;
        Scope = scope;

        if (type is ITypeDefinition typeDefinition)
        {
            IsNamedType = true;
            IsIntrospectionType = typeDefinition.IsIntrospectionType();
            Kind = typeDefinition.Kind;
        }
        else if (type is DirectiveType)
        {
            IsDirectiveType = true;
            Kind = TypeKind.Directive;
        }
    }

    public TypeSystemObject Type { get; }

    public TypeKind? Kind { get; }

    public Type RuntimeType
        => Type is IRuntimeTypeProvider hasClrType
            ? hasClrType.RuntimeType
            : typeof(object);

    public List<TypeReference> References { get; } = [];

    public List<TypeDependency> Dependencies { get; } = [];

    public List<TypeDependency> Conditionals => _conditionals ??= [];

    public bool IsInferred { get; }

    public bool IsExtension { get; }

    public bool IsNamedType { get; }

    public bool IsDirectiveType { get; }

    public bool IsIntrospectionType { get; }

    public bool IsSchema { get; }

    public bool IsType => IsNamedType;

    public bool IsDirective => IsDirectiveType;

    public List<ISchemaError> Errors => _errors ??= [];

    public bool HasErrors => _errors is { Count: > 0 };

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

        if (Type is INameProvider { Name: { Length: > 0 } name })
        {
            return IsDirective ? $"@{name}" : name;
        }

        return Type.ToString();
    }
}
