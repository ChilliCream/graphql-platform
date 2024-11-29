using HotChocolate.Utilities;

#nullable enable

namespace HotChocolate.Types.Descriptors.Definitions;

public class InterfaceTypeDefinition : TypeDefinitionBase, IComplexOutputTypeDefinition
{
    private List<Type>? _knownClrTypes;
    private List<TypeReference>? _interfaces;

    /// <summary>
    /// Initializes a new instance of <see cref="ObjectTypeDefinition"/>.
    /// </summary>
    public InterfaceTypeDefinition() { }

    /// <summary>
    /// Initializes a new instance of <see cref="ObjectTypeDefinition"/>.
    /// </summary>
    public InterfaceTypeDefinition(
        string name,
        string? description = null,
        Type? runtimeType = null)
        : base(runtimeType ?? typeof(object))
    {
        Name = name.EnsureGraphQLName();
        Description = description;
    }

    public IList<Type> KnownRuntimeTypes => _knownClrTypes ??= [];

    public ResolveAbstractType? ResolveAbstractType { get; set; }

    public IList<TypeReference> Interfaces => _interfaces ??= [];

    /// <summary>
    /// Specifies if this definition has interfaces.
    /// </summary>
    public bool HasInterfaces => _interfaces is { Count: > 0, };

    public IBindableList<InterfaceFieldDefinition> Fields { get; } =
        new BindableList<InterfaceFieldDefinition>();

    public override IEnumerable<ITypeSystemMemberConfiguration> GetConfigurations()
    {
        List<ITypeSystemMemberConfiguration>? configs = null;

        if (HasConfigurations)
        {
            configs ??= [];
            configs.AddRange(Configurations);
        }

        foreach (var field in Fields)
        {
            if (field.HasConfigurations)
            {
                configs ??= [];
                configs.AddRange(field.Configurations);
            }

            if (field.HasArguments)
            {
                foreach (var argument in field.Arguments)
                {
                    if (argument.HasConfigurations)
                    {
                        configs ??= [];
                        configs.AddRange(argument.Configurations);
                    }
                }
            }
        }

        return configs ?? Enumerable.Empty<ITypeSystemMemberConfiguration>();
    }

    internal IReadOnlyList<Type> GetKnownClrTypes()
    {
        if (_knownClrTypes is null)
        {
            return Array.Empty<Type>();
        }

        return _knownClrTypes;
    }

    internal IReadOnlyList<TypeReference> GetInterfaces()
    {
        if (_interfaces is null)
        {
            return Array.Empty<TypeReference>();
        }

        return _interfaces;
    }
}
