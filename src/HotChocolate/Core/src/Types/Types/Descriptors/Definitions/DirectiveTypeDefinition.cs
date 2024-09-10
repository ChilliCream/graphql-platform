using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Utilities;

#nullable enable

namespace HotChocolate.Types.Descriptors.Definitions;

/// <summary>
/// Defines the properties of a GraphQL directive.
/// </summary>
public class DirectiveTypeDefinition : DefinitionBase, IHasRuntimeType
{
    private Type _clrType = typeof(object);
    private List<DirectiveMiddleware>? _middlewareComponents;
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
    /// Gets or the associated middleware components.
    /// </summary>
    public IList<DirectiveMiddleware> MiddlewareComponents =>
        _middlewareComponents ??= [];

    /// <summary>
    /// Defines the location on which a directive can be annotated.
    /// </summary>
    public DirectiveLocation Locations { get; set; }

    /// <summary>
    /// Gets the directive arguments.
    /// </summary>
    public IBindableList<DirectiveArgumentDefinition> Arguments
        => _arguments ??= [];

    /// <summary>
    /// Specifies if this directive definition has an arguments.
    /// </summary>
    public bool HasArguments => _arguments is { Count: > 0, };

    /// <summary>
    /// Gets or sets the input object runtime value factory delegate.
    /// </summary>
    public Func<object?[], object>? CreateInstance { get; set; }

    /// <summary>
    /// Gets or sets the delegate to extract the field values from the runtime value.
    /// </summary>
    public Action<object, object?[]>? GetFieldData { get; set; }

    /// <summary>
    /// Gets or sets the delegate to parse a directive literal to an instance of this directive.
    /// </summary>
    public Func<DirectiveNode, object>? Parse { get; set; }

    /// <summary>
    /// Gets or sets the delegate to format an instance of this directive to a directive literal.
    /// </summary>
    public Func<object, DirectiveNode>? Format { get; set; }

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
    /// Gets or the associated middleware components.
    /// </summary>
    internal IReadOnlyList<DirectiveMiddleware> GetMiddlewareComponents()
    {
        if (_middlewareComponents is null)
        {
            return Array.Empty<DirectiveMiddleware>();
        }

        return _middlewareComponents;
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
