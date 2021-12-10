using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;

#nullable enable

namespace HotChocolate.Types.Descriptors.Definitions;

public class InterfaceTypeDefinition
    : TypeDefinitionBase<InterfaceTypeDefinitionNode>
    , IComplexOutputTypeDefinition
{
    private List<Type>? _knownClrTypes;
    private List<ITypeReference>? _interfaces;

    /// <summary>
    /// Initializes a new instance of <see cref="ObjectTypeDefinition"/>.
    /// </summary>
    public InterfaceTypeDefinition() { }

    /// <summary>
    /// Initializes a new instance of <see cref="ObjectTypeDefinition"/>.
    /// </summary>
    public InterfaceTypeDefinition(
        NameString name,
        string? description = null,
        Type? runtimeType = null)
        : base(runtimeType ?? typeof(object))
    {
        Name = name;
        Description = description;
    }

    public IList<Type> KnownRuntimeTypes => _knownClrTypes ??= new List<Type>();

    public ResolveAbstractType? ResolveAbstractType { get; set; }

    public IList<ITypeReference> Interfaces => _interfaces ??= new List<ITypeReference>();

    /// <summary>
    /// Specifies if this definition has interfaces.
    /// </summary>
    public bool HasInterfaces => _interfaces is { Count: > 0 };

    public IBindableList<InterfaceFieldDefinition> Fields { get; } =
        new BindableList<InterfaceFieldDefinition>();

    public override IEnumerable<ITypeSystemMemberConfiguration> GetConfigurations()
    {
        List<ITypeSystemMemberConfiguration>? configs = null;

        if (HasConfigurations)
        {
            configs ??= new();
            configs.AddRange(Configurations);
        }

        foreach (InterfaceFieldDefinition field in Fields)
        {
            if (field.HasConfigurations)
            {
                configs ??= new();
                configs.AddRange(field.Configurations);
            }

            if (field.HasArguments)
            {
                foreach (ArgumentDefinition argument in field.Arguments)
                {
                    if (argument.HasConfigurations)
                    {
                        configs ??= new();
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

    internal IReadOnlyList<ITypeReference> GetInterfaces()
    {
        if (_interfaces is null)
        {
            return Array.Empty<ITypeReference>();
        }

        return _interfaces;
    }
}
