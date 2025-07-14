using System.Runtime.CompilerServices;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Configurations;
using HotChocolate.Types.Helpers;
using static HotChocolate.Internal.FieldInitHelper;
using static HotChocolate.Serialization.SchemaDebugFormatter;

#nullable enable

namespace HotChocolate.Types;

public abstract class OutputField : FieldBase, IOutputFieldDefinition
{
    private Type _runtimeType = null!;

    protected OutputField(OutputFieldConfiguration configuration, int index)
        : base(configuration, index)
    {
    }

    protected OutputField(OutputField original, IType type)
        : base(original, type)
    {
    }

    /// <summary>
    /// Gets the type that declares this field.
    /// </summary>
    public new IComplexTypeDefinition DeclaringType => Unsafe.As<IComplexTypeDefinition>(base.DeclaringType);

    /// <inheritdoc />
    public new IOutputType Type => Unsafe.As<IOutputType>(base.Type);

    /// <inheritdoc />
    public override Type RuntimeType => _runtimeType;

    public ArgumentCollection Arguments { get; private set; } = ArgumentCollection.Empty;

    IReadOnlyFieldDefinitionCollection<IInputValueDefinition> IOutputFieldDefinition.Arguments
        => Arguments.AsReadOnlyFieldDefinitionCollection();

    /// <summary>
    /// Defines if this field as an introspection field.
    /// </summary>
    public bool IsIntrospectionField
        => (Flags & CoreFieldFlags.Introspection) == CoreFieldFlags.Introspection;

    internal bool IsTypeNameField
        => (Flags & CoreFieldFlags.TypeNameIntrospectionField) == CoreFieldFlags.TypeNameIntrospectionField;

    protected sealed override void OnCompleteField(
        ITypeCompletionContext context,
        ITypeSystemMember declaringMember,
        FieldConfiguration definition)
        => OnCompleteField(context, declaringMember, (OutputFieldConfiguration)definition);

    protected virtual void OnCompleteField(
        ITypeCompletionContext context,
        ITypeSystemMember declaringMember,
        OutputFieldConfiguration definition)
    {
        base.OnCompleteField(context, declaringMember, definition);
        _runtimeType = CompleteRuntimeType(Type, null);

        if (_runtimeType == typeof(object)
            && definition is ObjectFieldConfiguration { ResultType: not null } objectFieldCfg
            && objectFieldCfg.ResultType != typeof(object))
        {
            _runtimeType = CompleteRuntimeType(Type, objectFieldCfg.ResultType);
        }

        Arguments = OnCompleteArguments(context, definition);
    }

    protected virtual ArgumentCollection OnCompleteArguments(
        ITypeCompletionContext context,
        OutputFieldConfiguration definition)
    {
        return new ArgumentCollection(
            CompleteFields(
                context,
                this,
                definition.GetArguments(),
                CreateArgument));
        static Argument CreateArgument(ArgumentConfiguration argDef, int index)
            => new(argDef, index);
    }

    protected sealed override void OnCompleteMetadata(
        ITypeCompletionContext context,
        ITypeSystemMember declaringMember,
        FieldConfiguration definition)
        => OnCompleteMetadata(context, declaringMember, (OutputFieldConfiguration)definition);

    protected virtual void OnCompleteMetadata(
        ITypeCompletionContext context,
        ITypeSystemMember declaringMember,
        OutputFieldConfiguration definition)
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
        FieldConfiguration definition)
        => OnMakeExecutable(context, declaringMember, (OutputFieldConfiguration)definition);

    protected virtual void OnMakeExecutable(
        ITypeCompletionContext context,
        ITypeSystemMember declaringMember,
        OutputFieldConfiguration definition)
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
        FieldConfiguration definition)
        => OnFinalizeField(context, declaringMember, (OutputFieldConfiguration)definition);

    protected virtual void OnFinalizeField(
        ITypeCompletionContext context,
        ITypeSystemMember declaringMember,
        OutputFieldConfiguration definition)
    {
        base.OnFinalizeField(context, declaringMember, definition);

        foreach (IFieldCompletion argument in Arguments)
        {
            argument.Finalize(context, this);
        }
    }

    /// <summary>
    /// Creates a <see cref="FieldDefinitionNode"/> that represents the output field.
    /// </summary>
    /// <returns>
    /// The GraphQL syntax node that represents the output field.
    /// </returns>
    public FieldDefinitionNode ToSyntaxNode()
        => Format(this);

    /// <inheritdoc />
    protected override ISyntaxNode FormatField()
        => Format(this);
}
