using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Configurations;

namespace HotChocolate.Types;

/// <summary>
/// A base class for named GraphQL types.
/// </summary>
/// <typeparam name="TConfiguration">
/// The type configuration of the named GraphQL type.
/// </typeparam>
public abstract class NamedTypeBase<TConfiguration>
    : TypeSystemObject<TConfiguration>
    , ITypeDefinition
    , ITypeIdentityProvider
    , ITypeConfigurationProvider
    where TConfiguration : TypeSystemConfiguration, IDirectiveConfigurationProvider, ITypeConfiguration
{
    private DirectiveCollection? _directives;
    private Type? _runtimeType;

    ITypeConfiguration? ITypeConfigurationProvider.Configuration => Configuration;

    /// <inheritdoc />
    public abstract TypeKind Kind { get; }

    /// <summary>
    /// Gets the schema coordinate of the named type.
    /// </summary>
    public SchemaCoordinate Coordinate => new(Name, ofDirective: false);

    /// <inheritdoc />
    public DirectiveCollection Directives
    {
        get
        {
            if (_directives is null)
            {
                throw new TypeInitializationException();
            }

            return _directives;
        }

        // we allow internal type interceptors to set the directives before the type is completed.
        // this gets rid of the need to initialize the directives twice.
        internal set
        {
            if (_directives is not null)
            {
                throw new TypeInitializationException();
            }

            _directives = value;
        }
    }

    IReadOnlyDirectiveCollection IDirectivesProvider.Directives => Directives.AsReadOnlyDirectiveCollection();

    /// <inheritdoc />
    public Type RuntimeType
    {
        get
        {
            if (_runtimeType is null)
            {
                throw new TypeInitializationException();
            }
            return _runtimeType;
        }
    }

    /// <summary>
    /// A type representing the identity of the specified type.
    /// </summary>
    public Type? TypeIdentity { get; private set; }

    /// <inheritdoc />
    public virtual bool IsAssignableFrom(ITypeDefinition type)
        => ReferenceEquals(type, this);

    /// <inheritdoc />
    protected override void OnRegisterDependencies(
        ITypeDiscoveryContext context,
        TConfiguration configuration)
    {
        base.OnRegisterDependencies(context, configuration);

        UpdateRuntimeType(configuration);
    }

    /// <inheritdoc />
    protected override void OnCompleteType(
        ITypeCompletionContext context,
        TConfiguration configuration)
    {
        base.OnCompleteType(context, configuration);

        UpdateRuntimeType(configuration);
    }

    protected override void OnCompleteMetadata(
        ITypeCompletionContext context,
        TConfiguration configuration)
    {
        base.OnCompleteMetadata(context, configuration);

        _directives ??= DirectiveCollection.CreateAndComplete(
            context, this, configuration.GetDirectives());
    }

    /// <summary>
    /// This method allows the concrete type implementation to set its type identity.
    /// </summary>
    /// <param name="typeDefinitionOrIdentity">
    /// The type definition or type identity.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="typeDefinitionOrIdentity"/> is <c>null</c>.
    /// </exception>
    protected void SetTypeIdentity(Type typeDefinitionOrIdentity)
    {
        ArgumentNullException.ThrowIfNull(typeDefinitionOrIdentity);

        if (!typeDefinitionOrIdentity.IsGenericTypeDefinition)
        {
            TypeIdentity = typeDefinitionOrIdentity;
        }
        else if (RuntimeType != typeof(object))
        {
            TypeIdentity = typeDefinitionOrIdentity.MakeGenericType(RuntimeType);
        }
    }

    private void UpdateRuntimeType(ITypeConfiguration definition)
        => _runtimeType = definition.RuntimeType ?? typeof(object);

    public bool Equals(IType? other)
        => ReferenceEquals(this, other);

    /// <summary>
    /// Returns a string representation of the type.
    /// </summary>
    public sealed override string ToString()
        => FormatType().ToString();

    /// <summary>
    /// Returns a <see cref="ITypeDefinitionNode"/> from the named type.
    /// </summary>
    /// <returns></returns>
    public ITypeDefinitionNode ToSyntaxNode() => FormatType();

    ISyntaxNode ISyntaxNodeProvider.ToSyntaxNode() => FormatType();

    /// <summary>
    /// Creates a <see cref="ISyntaxNode"/> from a type system member.
    /// </summary>
    protected abstract ITypeDefinitionNode FormatType();
}
