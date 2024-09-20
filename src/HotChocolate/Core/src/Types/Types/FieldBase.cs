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
    private FieldDefinitionBase? _definition;
    private FieldFlags _flags;

    protected FieldBase(FieldDefinitionBase definition, int index)
    {
        _definition = definition ?? throw new ArgumentNullException(nameof(definition));
        Index = index;

        Name = definition.Name.EnsureGraphQLName();
        Description = definition.Description;
        Flags = definition.Flags;
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
        _definition = null;
        _flags |= FieldFlags.Sealed;
    }

    protected virtual void OnCompleteField(
        ITypeCompletionContext context,
        ITypeSystemMember declaringMember,
        FieldDefinitionBase definition)
    {
        DeclaringType = context.Type;
        Coordinate = declaringMember is IField field
            ? new SchemaCoordinate(context.Type.Name, field.Name, definition.Name)
            : new SchemaCoordinate(context.Type.Name, definition.Name);

        Directives = DirectiveCollection.CreateAndComplete(
            context, this, definition.GetDirectives());
        Flags = definition.Flags;
    }

    void IFieldCompletion.CompleteField(
        ITypeCompletionContext context,
        ITypeSystemMember declaringMember)
        => CompleteField(context, declaringMember);

    private void AssertMutable()
    {
        if ((_flags & FieldFlags.Sealed) == FieldFlags.Sealed)
        {
            throw ThrowHelper.FieldBase_Sealed();
        }
    }
}
