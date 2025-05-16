using HotChocolate.Configuration;
using HotChocolate.Features;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Types.Helpers;
using HotChocolate.Utilities;
using ThrowHelper = HotChocolate.Utilities.ThrowHelper;

#nullable enable

namespace HotChocolate.Types;

public abstract class FieldBase
    : IFieldDefinition
    , IFieldCompletion
    , IHasFieldIndex
    , IHasRuntimeType
{
    private FieldConfiguration? _config;
    private CoreFieldFlags _coreFlags;

    protected FieldBase(FieldConfiguration configuration, int index)
    {
        _config = configuration ?? throw new ArgumentNullException(nameof(configuration));
        Index = index;

        Name = configuration.Name.EnsureGraphQLName();
        Description = configuration.Description;
        IsDeprecated = !string.IsNullOrEmpty(configuration.DeprecationReason);
        DeprecationReason = configuration.DeprecationReason;
        Flags = configuration.Flags;
        DeclaringType = default!;
        DeclaringMember = default!;
        Features = default!;
        Directives = default!;
        Type = default!;
    }

    protected FieldBase(FieldBase original, IType type)
    {
        ArgumentNullException.ThrowIfNull(original);
        ArgumentNullException.ThrowIfNull(type);

        _config = original._config;
        Index = original.Index;
        Name = original.Name;
        Description = original.Description;
        IsDeprecated = original.IsDeprecated;
        DeprecationReason = original.DeprecationReason;
        Flags = original.Flags;
        DeclaringType = original.DeclaringType;
        DeclaringMember = original.DeclaringMember;
        Features = original.Features;
        Directives = original.Directives;
        Type = type;
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public string? Description { get; }

    /// <summary>
    /// Gets the type that declares this field.
    /// </summary>
    public ITypeDefinition DeclaringType { get; private set; }

    /// <inheritdoc />
    public ITypeSystemMember DeclaringMember { get; private set; }

    /// <inheritdoc />
    public SchemaCoordinate Coordinate { get; private set; }

    /// <summary>
    /// Gets the index of this field in the declaring members field collection.
    /// </summary>
    public int Index { get; }

    /// <inheritdoc />
    public DirectiveCollection Directives { get; private set; }

    IReadOnlyDirectiveCollection IDirectivesProvider.Directives
        => Directives.AsReadOnlyDirectiveCollection();

    /// <inheritdoc />
    public bool IsDeprecated { get; }

    /// <inheritdoc />
    public string? DeprecationReason { get; }

    /// <inheritdoc />
    public IType Type { get; private set; }

    /// <inheritdoc />
    public abstract Type RuntimeType { get; }

    internal CoreFieldFlags Flags
    {
        get => _coreFlags;
        set
        {
            AssertMutable();
            _coreFlags = value;
        }
    }

    FieldFlags IFieldDefinition.Flags => FieldFlagsMapper.MapToPublic(_coreFlags);

    /// <inheritdoc />
    public IFeatureCollection Features { get; private set; }

    internal void CompleteField(
        ITypeCompletionContext context,
        ITypeSystemMember declaringMember)
    {
        AssertMutable();
        OnCompleteField(context, declaringMember, _config!);
        Features = _config!.GetFeatures();
    }

    protected virtual void OnCompleteField(
        ITypeCompletionContext context,
        ITypeSystemMember declaringMember,
        FieldConfiguration definition)
    {
        DeclaringType = (ITypeDefinition)context.Type;
        DeclaringMember = context.Type;
        Flags = definition.Flags;

        if (declaringMember is IFieldDefinition field)
        {
            DeclaringMember = field;
            Coordinate = new SchemaCoordinate(context.Type.Name, field.Name, definition.Name);
        }
        else
        {
            Coordinate = new SchemaCoordinate(context.Type.Name, definition.Name);
        }

        Type = context.GetType<IInputType>(definition.Type!).EnsureInputType();
    }

    void IFieldCompletion.CompleteField(
        ITypeCompletionContext context,
        ITypeSystemMember declaringMember)
        => CompleteField(context, declaringMember);

    private void CompleteMetadata(
        ITypeCompletionContext context,
        ITypeSystemMember declaringMember)
    {
        AssertMutable();
        OnCompleteMetadata(context, declaringMember, _config!);
    }

    protected virtual void OnCompleteMetadata(
        ITypeCompletionContext context,
        ITypeSystemMember declaringMember,
        FieldConfiguration definition)
    {
        Directives =
            DirectiveCollection.CreateAndComplete(
                context,
                this,
                definition.GetDirectives());
    }

    void IFieldCompletion.CompleteMetadata(
        ITypeCompletionContext context,
        ITypeSystemMember declaringMember)
        => CompleteMetadata(context, declaringMember);

    private void MakeExecutable(
        ITypeCompletionContext context,
        ITypeSystemMember declaringMember)
    {
        AssertMutable();
        OnMakeExecutable(context, declaringMember, _config!);
    }

    protected virtual void OnMakeExecutable(
        ITypeCompletionContext context,
        ITypeSystemMember declaringMember,
        FieldConfiguration definition)
    {
    }

    void IFieldCompletion.MakeExecutable(
        ITypeCompletionContext context,
        ITypeSystemMember declaringMember)
        => MakeExecutable(context, declaringMember);

    private void FinalizeField(
        ITypeCompletionContext context,
        ITypeSystemMember declaringMember)
    {
        AssertMutable();
        OnFinalizeField(context, declaringMember, _config!);
        Features = Features.ToReadOnly();
        _config = null;
        _coreFlags |= CoreFieldFlags.Sealed;
    }

    protected virtual void OnFinalizeField(
        ITypeCompletionContext context,
        ITypeSystemMember declaringMember,
        FieldConfiguration definition)
    {
    }

    void IFieldCompletion.Finalize(
        ITypeCompletionContext context,
        ITypeSystemMember declaringMember)
        => FinalizeField(context, declaringMember);

    private void AssertMutable()
    {
        if ((_coreFlags & CoreFieldFlags.Sealed) == CoreFieldFlags.Sealed)
        {
            throw ThrowHelper.FieldBase_Sealed();
        }
    }

    /// <summary>
    /// Returns a string representation of the field.
    /// </summary>
    public sealed override string ToString()
        => FormatField().ToString();

    ISyntaxNode ISyntaxNodeProvider.ToSyntaxNode()
        => FormatField();

    /// <summary>
    /// Creates a <see cref="ISyntaxNode"/> from a type system member.
    /// </summary>
    protected abstract ISyntaxNode FormatField();
}
