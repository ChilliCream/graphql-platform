using System.Reflection;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Definitions;
using static HotChocolate.Types.MemberKind;

#nullable enable

namespace HotChocolate.Types.Descriptors;

public class InputFieldDescriptor
    : ArgumentDescriptorBase<InputFieldDefinition>
    , IInputFieldDescriptor
{
    /// <summary>
    ///  Creates a new instance of <see cref="InputFieldDescriptor"/>
    /// </summary>
    protected internal InputFieldDescriptor(
        IDescriptorContext context,
        string fieldName)
        : base(context)
    {
        Definition.Name = fieldName;
    }

    /// <summary>
    ///  Creates a new instance of <see cref="InputFieldDescriptor"/>
    /// </summary>
    protected internal InputFieldDescriptor(
        IDescriptorContext context,
        InputFieldDefinition definition)
        : base(context)
    {
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
    }

    /// <summary>
    ///  Creates a new instance of <see cref="InputFieldDescriptor"/>
    /// </summary>
    protected internal InputFieldDescriptor(
        IDescriptorContext context,
        PropertyInfo property)
        : base(context)
    {
        Definition.Property = property
            ?? throw new ArgumentNullException(nameof(property));
        Definition.Name = context.Naming.GetMemberName(property, InputObjectField);
        Definition.Description = context.Naming.GetMemberDescription(property, InputObjectField);
        Definition.Type = context.TypeInspector.GetInputReturnTypeRef(property);

        if (context.TypeInspector.TryGetDefaultValue(property, out var defaultValue))
        {
            Definition.RuntimeDefaultValue = defaultValue;
        }

        if (context.Naming.IsDeprecated(property, out var reason))
        {
            Deprecated(reason);
        }
    }

    /// <inheritdoc />
    protected override void OnCreateDefinition(InputFieldDefinition definition)
    {
        Context.Descriptors.Push(this);

        if (Definition is { AttributesAreApplied: false, Property: not null, })
        {
            Context.TypeInspector.ApplyAttributes(
                Context,
                this,
                Definition.Property);
            Definition.AttributesAreApplied = true;
        }

        base.OnCreateDefinition(definition);

        Context.Descriptors.Pop();
    }

    /// <inheritdoc />
    public IInputFieldDescriptor Name(string value)
    {
        Definition.Name = value;
        return this;
    }

    /// <inheritdoc />
    public new IInputFieldDescriptor Deprecated(string? reason)
    {
        base.Deprecated(reason);
        return this;
    }

    /// <inheritdoc />
    public new IInputFieldDescriptor Deprecated()
    {
        base.Deprecated();
        return this;
    }

    /// <inheritdoc />
    public new IInputFieldDescriptor Description(string value)
    {
        base.Description(value);
        return this;
    }

    /// <inheritdoc />
    public new IInputFieldDescriptor Type<TInputType>()
        where TInputType : IInputType
    {
        base.Type<TInputType>();
        return this;
    }

    /// <inheritdoc />
    public new IInputFieldDescriptor Type<TInputType>(TInputType inputType)
        where TInputType : class, IInputType
    {
        base.Type(inputType);
        return this;
    }

    /// <inheritdoc />
    public new IInputFieldDescriptor Type(ITypeNode typeNode)
    {
        base.Type(typeNode);
        return this;
    }

    /// <inheritdoc />
    public new IInputFieldDescriptor Type(Type type)
    {
        base.Type(type);
        return this;
    }

    /// <inheritdoc />
    public new IInputFieldDescriptor DefaultValue(IValueNode value)
    {
        base.DefaultValue(value);
        return this;
    }

    /// <inheritdoc />
    public new IInputFieldDescriptor DefaultValue(object value)
    {
        base.DefaultValue(value);
        return this;
    }

    /// <inheritdoc />
    public IInputFieldDescriptor Ignore(bool ignore = true)
    {
        Definition.Ignore = ignore;
        return this;
    }

    /// <inheritdoc />
    public new IInputFieldDescriptor Directive<TDirective>(TDirective directiveInstance)
        where TDirective : class
    {
        base.Directive(directiveInstance);
        return this;
    }

    /// <inheritdoc />
    public new IInputFieldDescriptor Directive<TDirective>()
        where TDirective : class, new()
    {
        base.Directive<TDirective>();
        return this;
    }

    /// <inheritdoc />
    public new IInputFieldDescriptor Directive(
        string name,
        params ArgumentNode[] arguments)
    {
        base.Directive(name, arguments);
        return this;
    }

    /// <summary>
    /// Creates a new instance of <see cref="InputFieldDescriptor "/>
    /// </summary>
    /// <param name="context">The descriptor context</param>
    /// <param name="fieldName">The name of the field</param>
    /// <returns>An instance of <see cref="InputFieldDescriptor "/></returns>
    public static InputFieldDescriptor New(
        IDescriptorContext context,
        string fieldName) =>
        new(context, fieldName);

    /// <summary>
    /// Creates a new instance of <see cref="InputFieldDescriptor "/>
    /// </summary>
    /// <param name="context">The descriptor context</param>
    /// <param name="property">The property this parameter is used for</param>
    /// <returns>An instance of <see cref="InputFieldDescriptor "/></returns>
    public static InputFieldDescriptor New(
        IDescriptorContext context,
        PropertyInfo property) =>
        new(context, property);

    /// <summary>
    /// Creates a new instance of <see cref="InputFieldDescriptor "/>
    /// </summary>
    /// <param name="context">The descriptor context</param>
    /// <param name="definition">The definition of the argument</param>
    /// <returns>An instance of <see cref="InputFieldDescriptor "/></returns>
    public static InputFieldDescriptor From(
        IDescriptorContext context,
        InputFieldDefinition definition) =>
        new(context, definition);
}
