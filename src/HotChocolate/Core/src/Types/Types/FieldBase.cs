using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Types.Helpers;
using HotChocolate.Utilities;
using ThrowHelper = HotChocolate.Utilities.ThrowHelper;

#nullable enable

namespace HotChocolate.Types;

public abstract class FieldBase
    : IField
    , IFieldCompletion
{
    private FieldConfiguration? _definition;
    private FieldFlags _flags;

    protected FieldBase(FieldConfiguration configuration, int index)
    {
        _definition = configuration ?? throw new ArgumentNullException(nameof(configuration));
        Index = index;

        Name = configuration.Name.EnsureGraphQLName();
        Description = configuration.Description;
        Flags = configuration.Flags;
        DeclaringType = default!;
        ContextData = default!;
        Directives = default!;
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public string? Description { get; }

    /// <inheritdoc />
    public ITypeSystemObject DeclaringType { get; private set; }

    /// <inheritdoc />
    public SchemaCoordinate Coordinate { get; private set; }

    /// <inheritdoc />
    public int Index { get; }

    /// <inheritdoc />
    public IDirectiveCollection Directives { get; private set; }

    /// <inheritdoc />
    public abstract Type RuntimeType { get; }

    internal FieldFlags Flags
    {
        get => _flags;
        set
        {
            AssertMutable();
            _flags = value;
        }
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, object?> ContextData { get; private set; }

    internal void CompleteField(
        ITypeCompletionContext context,
        ITypeSystemMember declaringMember)
    {
        AssertMutable();
        OnCompleteField(context, declaringMember, _definition!);
        ContextData = _definition!.GetContextData();
    }

    protected virtual void OnCompleteField(
        ITypeCompletionContext context,
        ITypeSystemMember declaringMember,
        FieldConfiguration definition)
    {
        DeclaringType = context.Type;
        Coordinate = declaringMember is IField field
            ? new SchemaCoordinate(context.Type.Name, field.Name, definition.Name)
            : new SchemaCoordinate(context.Type.Name, definition.Name);
        Flags = definition.Flags;
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
        OnCompleteMetadata(context, declaringMember, _definition!);
    }

    protected virtual void OnCompleteMetadata(
        ITypeCompletionContext context,
        ITypeSystemMember declaringMember,
        FieldConfiguration definition)
    {
        Directives = DirectiveCollection.CreateAndComplete(
            context, this, definition.GetDirectives());
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
        OnMakeExecutable(context, declaringMember, _definition!);
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
        OnFinalizeField(context, declaringMember, _definition!);
        _definition = null;
        _flags |= FieldFlags.Sealed;
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
        if ((_flags & FieldFlags.Sealed) == FieldFlags.Sealed)
        {
            throw ThrowHelper.FieldBase_Sealed();
        }
    }
}
