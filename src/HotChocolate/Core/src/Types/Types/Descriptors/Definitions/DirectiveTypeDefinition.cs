using System;
using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Utilities;

#nullable enable

namespace HotChocolate.Types.Descriptors.Definitions;

/// <summary>
/// Defines the properties of a GraphQL directive.
/// </summary>
public class DirectiveTypeDefinition
    : DefinitionBase<DirectiveDefinitionNode>
    , IHasRuntimeType
{
    private Type _clrType = typeof(object);
    private HashSet<DirectiveLocation>? _locations;
    private BindableList<DirectiveArgumentDefinition>? _arguments;

    /// <summary>
    /// Initializes a new instance of <see cref="DirectiveTypeDefinition"/>.
    /// </summary>
    public DirectiveTypeDefinition() { }

    /// <summary>
    /// Initializes a new instance of <see cref="DirectiveTypeDefinition"/>.
    /// </summary>
    public DirectiveTypeDefinition(
        string name,
        string? description = null,
        Type? runtimeType = null,
        bool isRepeatable = false)
    {
        Name = name.EnsureGraphQLName();
        Description = description;
        RuntimeType = runtimeType ?? typeof(object);
        IsRepeatable = isRepeatable;
    }

    /// <summary>
    /// Defines if this directive can be specified multiple
    /// times on the same object.
    /// </summary>
    public bool IsRepeatable { get; set; }

    /// <summary>
    /// Defines if this directive is visible when directive introspection is enabled.
    /// </summary>
    public bool IsPublic { get; set; }

    /// <summary>
    /// Gets or sets the .net type representation of this directive.
    /// </summary>
    public Type RuntimeType
    {
        get => _clrType;
        set
        {
            _clrType = value ?? throw new ArgumentNullException(nameof(value));
        }
    }

    /// <summary>
    /// Defines the location on which a directive can be annotated.
    /// </summary>
    public ISet<DirectiveLocation> Locations => _locations ??= new HashSet<DirectiveLocation>();

    /// <summary>
    /// Gets the directive arguments.
    /// </summary>
    public IBindableList<DirectiveArgumentDefinition> Arguments =>
        _arguments ??= new BindableList<DirectiveArgumentDefinition>();

    public bool HasArguments => _arguments is { Count: > 0 };

    public override IEnumerable<ITypeSystemMemberConfiguration> GetConfigurations()
    {
        var configs = new List<ITypeSystemMemberConfiguration>();

        configs.AddRange(Configurations);

        foreach (var field in GetArguments())
        {
            configs.AddRange(field.Configurations);
        }

        return configs;
    }

    /// <summary>
    /// Defines the location on which a directive can be annotated.
    /// </summary>
    internal IReadOnlyCollection<DirectiveLocation> GetLocations()
    {
        if (_locations is null)
        {
            return Array.Empty<DirectiveLocation>();
        }

        return _locations;
    }

    /// <summary>
    /// Gets the directive arguments.
    /// </summary>
    internal IReadOnlyList<DirectiveArgumentDefinition> GetArguments()
    {
        if (_arguments is null)
        {
            return Array.Empty<DirectiveArgumentDefinition>();
        }

        return _arguments;
    }
}
