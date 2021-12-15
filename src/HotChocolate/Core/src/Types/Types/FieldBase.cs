using System;
using System.Collections.Generic;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Types.Helpers;

#nullable enable

namespace HotChocolate.Types;

public abstract class FieldBase<TDefinition>
    : IField
    , IFieldCompletion
    where TDefinition : FieldDefinitionBase, IHasSyntaxNode
{
    private TDefinition? _definition;

    protected FieldBase(TDefinition definition, int index)
    {
        _definition = definition ?? throw new ArgumentNullException(nameof(definition));
        Index = index;

        SyntaxNode = definition.SyntaxNode;
        Name = definition.Name.EnsureNotEmpty(nameof(definition.Name));
        Description = definition.Description;
        DeclaringType = default!;
        ContextData = default!;
        Directives = default!;
    }

    /// <inheritdoc />
    public NameString Name { get; }

    /// <inheritdoc />
    public string? Description { get; }

    /// <inheritdoc />
    public ISyntaxNode? SyntaxNode { get; }

    /// <inheritdoc />
    public ITypeSystemObject DeclaringType { get; private set; }

    /// <inheritdoc />
    public FieldCoordinate Coordinate { get; private set; }

    /// <inheritdoc />
    public int Index { get; }

    /// <inheritdoc />
    public IDirectiveCollection Directives { get; private set; }

    /// <inheritdoc />
    public abstract Type RuntimeType { get; }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, object?> ContextData { get; private set; }

    internal void CompleteField(
        ITypeCompletionContext context,
        ITypeSystemMember declaringMember)
    {
        OnCompleteField(context, declaringMember, _definition!);

        ContextData = _definition!.GetContextData();
        _definition = null;
    }

    protected virtual void OnCompleteField(
        ITypeCompletionContext context,
        ITypeSystemMember declaringMember,
        TDefinition definition)
    {
        DeclaringType = context.Type;
        Coordinate = declaringMember is IField field
            ? new FieldCoordinate(context.Type.Name, field.Name, definition.Name)
            : new FieldCoordinate(context.Type.Name, definition.Name);
        Directives = DirectiveCollection.CreateAndComplete(
            context, this, definition.GetDirectives());
    }

    void IFieldCompletion.CompleteField(
        ITypeCompletionContext context,
        ITypeSystemMember declaringMember)
        => CompleteField(context, declaringMember);
}
