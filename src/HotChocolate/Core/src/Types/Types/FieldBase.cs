using HotChocolate.Configuration;
using HotChocolate.Features;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Configurations;
using HotChocolate.Types.Helpers;
using HotChocolate.Utilities;
using ThrowHelper = HotChocolate.Utilities.ThrowHelper;

namespace HotChocolate.Types;

/// <summary>
/// Represents the base class for all field implementations in the type system.
/// This class provides common functionality for object fields, interface fields, and input fields.
/// </summary>
public abstract class FieldBase
    : IFieldDefinition
    , IFieldCompletion
    , IFieldIndexProvider
    , IRuntimeTypeProvider
{
    private FieldConfiguration? _config;
    private CoreFieldFlags _coreFlags;

    /// <summary>
    /// Initializes a new instance of the <see cref="FieldBase"/> class.
    /// </summary>
    /// <param name="configuration">
    /// The field configuration containing the field's metadata.
    /// </param>
    /// <param name="index">
    /// The index of this field in the declaring type's field collection.
    /// </param>
    protected FieldBase(FieldConfiguration configuration, int index)
    {
        _config = configuration ?? throw new ArgumentNullException(nameof(configuration));
        Index = index;

        Name = configuration.Name.EnsureGraphQLName();
        Description = configuration.Description;
        IsDeprecated = !string.IsNullOrEmpty(configuration.DeprecationReason);
        DeprecationReason = configuration.DeprecationReason;
        Flags = configuration.Flags;
        DeclaringType = null!;
        DeclaringMember = null!;
        Features = null!;
        Directives = null!;
        Type = null!;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FieldBase"/> class by copying
    /// from an original field and applying a new type.
    /// </summary>
    /// <param name="original">
    /// The original field to copy from.
    /// </param>
    /// <param name="type">
    /// The new type to apply to this field.
    /// </param>
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
    public ITypeSystemMember DeclaringType { get; private set; }

    /// <inheritdoc />
    public ITypeSystemMember DeclaringMember { get; private set; }

    /// <inheritdoc />
    public SchemaCoordinate Coordinate { get; private set; }

    /// <summary>
    /// Gets the index of this field in the declaring members field collection.
    /// </summary>
    public int Index { get; }

    /// <summary>
    /// Gets the directives that are applied to this field.
    /// </summary>
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

    /// <summary>
    /// Completes the field initialization during the type system's completion phase.
    /// </summary>
    /// <param name="context">
    /// The type completion context.
    /// </param>
    /// <param name="declaringMember">
    /// The type system member that declares this field.
    /// </param>
    internal void CompleteField(
        ITypeCompletionContext context,
        ITypeSystemMember declaringMember)
    {
        AssertMutable();
        OnCompleteField(context, declaringMember, _config!);
        Features = _config!.GetFeatures();
    }

    /// <summary>
    /// Called during field completion to allow derived classes to complete field-specific logic.
    /// </summary>
    /// <param name="context">
    /// The type completion context.
    /// </param>
    /// <param name="declaringMember">
    /// The type system member that declares this field.
    /// </param>
    /// <param name="definition">
    /// The field configuration.
    /// </param>
    protected virtual void OnCompleteField(
        ITypeCompletionContext context,
        ITypeSystemMember declaringMember,
        FieldConfiguration definition)
    {
        DeclaringType = context.Type;
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

        Type = context.GetType<IType>(definition.Type!);
    }

    void IFieldCompletion.CompleteField(
        ITypeCompletionContext context,
        ITypeSystemMember declaringMember)
        => CompleteField(context, declaringMember);

    /// <summary>
    /// Completes the field's metadata (directives) during the type system's completion phase.
    /// </summary>
    /// <param name="context">
    /// The type completion context.
    /// </param>
    /// <param name="declaringMember">
    /// The type system member that declares this field.
    /// </param>
    private void CompleteMetadata(
        ITypeCompletionContext context,
        ITypeSystemMember declaringMember)
    {
        AssertMutable();
        OnCompleteMetadata(context, declaringMember, _config!);
    }

    /// <summary>
    /// Called during metadata completion to allow derived classes to complete metadata-specific logic.
    /// </summary>
    /// <param name="context">
    /// The type completion context.
    /// </param>
    /// <param name="declaringMember">
    /// The type system member that declares this field.
    /// </param>
    /// <param name="definition">
    /// The field configuration.
    /// </param>
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

    /// <summary>
    /// Prepares the field for execution during the type system's completion phase.
    /// </summary>
    /// <param name="context">
    /// The type completion context.
    /// </param>
    /// <param name="declaringMember">
    /// The type system member that declares this field.
    /// </param>
    private void MakeExecutable(
        ITypeCompletionContext context,
        ITypeSystemMember declaringMember)
    {
        AssertMutable();
        OnMakeExecutable(context, declaringMember, _config!);
    }

    /// <summary>
    /// Called during the make executable phase to allow derived classes to prepare for execution.
    /// </summary>
    /// <param name="context">
    /// The type completion context.
    /// </param>
    /// <param name="declaringMember">
    /// The type system member that declares this field.
    /// </param>
    /// <param name="definition">
    /// The field configuration.
    /// </param>
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

    /// <summary>
    /// Finalizes the field and seals it from further modifications.
    /// </summary>
    /// <param name="context">
    /// The type completion context.
    /// </param>
    /// <param name="declaringMember">
    /// The type system member that declares this field.
    /// </param>
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

    /// <summary>
    /// Called during finalization to allow derived classes to perform final setup before the field is sealed.
    /// </summary>
    /// <param name="context">
    /// The type completion context.
    /// </param>
    /// <param name="declaringMember">
    /// The type system member that declares this field.
    /// </param>
    /// <param name="definition">
    /// The field configuration.
    /// </param>
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

    /// <summary>
    /// Asserts that the field is still mutable and has not been sealed.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the field has already been sealed and cannot be modified.
    /// </exception>
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
