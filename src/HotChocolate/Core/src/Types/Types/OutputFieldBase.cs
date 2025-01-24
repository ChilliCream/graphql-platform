using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Types.Helpers;
using static HotChocolate.Internal.FieldInitHelper;

#nullable enable

namespace HotChocolate.Types;

public class OutputFieldBase : FieldBase, IOutputField
{
    private Type _runtimeType = default!;

    internal OutputFieldBase(OutputFieldDefinitionBase definition, int index)
        : base(definition, index)
    {
        DeprecationReason = definition.DeprecationReason;
    }

    /// <inheritdoc />
    public new IComplexOutputType DeclaringType => (IComplexOutputType)base.DeclaringType;

    /// <inheritdoc />
    public IOutputType Type { get; private set; } = default!;

    /// <inheritdoc />
    public override Type RuntimeType => _runtimeType;

    public FieldCollection<Argument> Arguments { get; private set; } =
        FieldCollection<Argument>.Empty;

    IFieldCollection<IInputField> IOutputFieldInfo.Arguments => Arguments;

    /// <summary>
    /// Defines if this field as an introspection field.
    /// </summary>
    public bool IsIntrospectionField
        => (Flags & FieldFlags.Introspection) == FieldFlags.Introspection;

    internal bool IsTypeNameField
        => (Flags & FieldFlags.TypeNameField) == FieldFlags.TypeNameField;

    /// <inheritdoc />
    public bool IsDeprecated
        => (Flags & FieldFlags.Deprecated) == FieldFlags.Deprecated;

    /// <inheritdoc />
    public string? DeprecationReason { get; }

    protected sealed override void OnCompleteField(
        ITypeCompletionContext context,
        ITypeSystemMember declaringMember,
        FieldDefinitionBase definition)
        => OnCompleteField(context, declaringMember, (OutputFieldDefinitionBase)definition);

    protected virtual void OnCompleteField(
        ITypeCompletionContext context,
        ITypeSystemMember declaringMember,
        OutputFieldDefinitionBase definition)
    {
        base.OnCompleteField(context, declaringMember, definition);

        Type = context.GetType<IOutputType>(definition.Type!).EnsureOutputType();
        _runtimeType = CompleteRuntimeType(Type, null);
        Arguments = OnCompleteFields(context, definition);
    }

    // TODO: V15: should be renamed to OnCompleteArguments
    protected virtual FieldCollection<Argument> OnCompleteFields(
        ITypeCompletionContext context,
        OutputFieldDefinitionBase definition)
    {
        return CompleteFields(context, this, definition.GetArguments(), CreateArgument);
        static Argument CreateArgument(ArgumentDefinition argDef, int index)
            => new(argDef, index);
    }

    protected sealed override void OnCompleteMetadata(
        ITypeCompletionContext context,
        ITypeSystemMember declaringMember,
        FieldDefinitionBase definition)
        => OnCompleteMetadata(context, declaringMember, (OutputFieldDefinitionBase)definition);

    protected virtual void OnCompleteMetadata(
        ITypeCompletionContext context,
        ITypeSystemMember declaringMember,
        OutputFieldDefinitionBase definition)
    {
        base.OnCompleteMetadata(context, declaringMember, definition);

        foreach (IFieldCompletion argument in Arguments)
        {
            argument.CompleteMetadata(context, this);
        }
    }

    protected sealed override void OnMakeExecutable(
        ITypeCompletionContext context,
        ITypeSystemMember declaringMember,
        FieldDefinitionBase definition)
        => OnMakeExecutable(context, declaringMember, (OutputFieldDefinitionBase)definition);

    protected virtual void OnMakeExecutable(
        ITypeCompletionContext context,
        ITypeSystemMember declaringMember,
        OutputFieldDefinitionBase definition)
    {
        base.OnMakeExecutable(context, declaringMember, definition);

        foreach (IFieldCompletion argument in Arguments)
        {
            argument.MakeExecutable(context, this);
        }
    }

    protected sealed override void OnFinalizeField(
        ITypeCompletionContext context,
        ITypeSystemMember declaringMember,
        FieldDefinitionBase definition)
        => OnFinalizeField(context, declaringMember, (OutputFieldDefinitionBase)definition);

    protected virtual void OnFinalizeField(
        ITypeCompletionContext context,
        ITypeSystemMember declaringMember,
        OutputFieldDefinitionBase definition)
    {
        base.OnFinalizeField(context, declaringMember, definition);

        foreach (IFieldCompletion argument in Arguments)
        {
            argument.Finalize(context, this);
        }
    }

    /// <summary>
    /// Returns a string that represents the current field.
    /// </summary>
    /// <returns>
    /// A string that represents the current field.
    /// </returns>
    public override string ToString() => $"{Name}:{Type.Print()}";
}
