using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors.Definitions;
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
    /// Defines if this field as a introspection field.
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

    protected virtual FieldCollection<Argument> OnCompleteFields(
        ITypeCompletionContext context,
        OutputFieldDefinitionBase definition)
    {
        return CompleteFields(context, this, definition.GetArguments(), CreateArgument);
        static Argument CreateArgument(ArgumentDefinition argDef, int index)
            => new(argDef, index);
    }

    /// <summary>
    /// Returns a string that represents the current field.
    /// </summary>
    /// <returns>
    /// A string that represents the current field.
    /// </returns>
    public override string ToString() => $"{Name}:{Type.Print()}";
}
