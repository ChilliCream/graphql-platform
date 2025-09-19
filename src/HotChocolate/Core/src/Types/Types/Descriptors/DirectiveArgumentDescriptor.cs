using System.Reflection;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Configurations;

namespace HotChocolate.Types.Descriptors;

/// <summary>
/// A fluent configuration API for GraphQL directive arguments.
/// </summary>
public class DirectiveArgumentDescriptor
    : ArgumentDescriptorBase<DirectiveArgumentConfiguration>
    , IDirectiveArgumentDescriptor
{
    /// <summary>
    ///  Creates a new instance of <see cref="DirectiveArgumentDescriptor"/>
    /// </summary>
    protected internal DirectiveArgumentDescriptor(
        IDescriptorContext context,
        string argumentName)
        : base(context)
    {
        Configuration.Name = argumentName;
    }

    /// <summary>
    ///  Creates a new instance of <see cref="DirectiveArgumentDescriptor"/>
    /// </summary>
    protected internal DirectiveArgumentDescriptor(
        IDescriptorContext context,
        PropertyInfo property)
        : base(context)
    {
        Configuration.Name = context.Naming.GetMemberName(
            property, MemberKind.DirectiveArgument);
        Configuration.Description = context.Naming.GetMemberDescription(
            property, MemberKind.DirectiveArgument);
        Configuration.Type = context.TypeInspector.GetInputReturnTypeRef(property);
        Configuration.Property = property;

        if (context.TypeInspector.TryGetDefaultValue(property, out var defaultValue))
        {
            Configuration.RuntimeDefaultValue = defaultValue;
        }

        if (context.Naming.IsDeprecated(property, out var reason))
        {
            Deprecated(reason);
        }
    }

    /// <summary>
    ///  Creates a new instance of <see cref="DirectiveArgumentDescriptor"/>
    /// </summary>
    protected internal DirectiveArgumentDescriptor(
        IDescriptorContext context,
        DirectiveArgumentConfiguration definition)
        : base(context)
    {
        Configuration = definition ?? throw new ArgumentNullException(nameof(definition));
    }

    /// <inheritdoc />
    protected override void OnCreateConfiguration(DirectiveArgumentConfiguration definition)
    {
        Context.Descriptors.Push(this);

        if (Configuration is { AttributesAreApplied: false, Property: not null })
        {
            Context.TypeInspector.ApplyAttributes(Context, this, Configuration.Property);
            Configuration.AttributesAreApplied = true;
        }

        base.OnCreateConfiguration(definition);

        Context.Descriptors.Pop();
    }

    /// <inheritdoc />
    public IDirectiveArgumentDescriptor Name(string value)
    {
        Configuration.Name = value;
        return this;
    }

    /// <inheritdoc />
    public new IDirectiveArgumentDescriptor Deprecated(string? reason)
    {
        base.Deprecated(reason);
        return this;
    }

    /// <inheritdoc />
    public new IDirectiveArgumentDescriptor Deprecated()
    {
        base.Deprecated();
        return this;
    }

    /// <inheritdoc />
    public new IDirectiveArgumentDescriptor Description(string value)
    {
        base.Description(value);
        return this;
    }

    /// <inheritdoc />
    public new IDirectiveArgumentDescriptor Type<TInputType>()
        where TInputType : IInputType
    {
        base.Type<TInputType>();
        return this;
    }

    /// <inheritdoc />
    public new IDirectiveArgumentDescriptor Type<TInputType>(TInputType inputType)
        where TInputType : class, IInputType
    {
        base.Type(inputType);
        return this;
    }

    /// <inheritdoc />
    public new IDirectiveArgumentDescriptor Type(ITypeNode typeNode)
    {
        base.Type(typeNode);
        return this;
    }

    /// <inheritdoc />
    public new IDirectiveArgumentDescriptor Type(Type type)
    {
        base.Type(type);
        return this;
    }

    /// <inheritdoc />
    public new IDirectiveArgumentDescriptor DefaultValue(IValueNode value)
    {
        base.DefaultValue(value);
        return this;
    }

    /// <inheritdoc />
    public new IDirectiveArgumentDescriptor DefaultValue(object value)
    {
        base.DefaultValue(value);
        return this;
    }

    /// <inheritdoc />
    public IDirectiveArgumentDescriptor Ignore(bool ignore = true)
    {
        Configuration.Ignore = ignore;
        return this;
    }

    /// <summary>
    /// Creates a new instance of <see cref="DirectiveArgumentDescriptor "/>
    /// </summary>
    /// <param name="context">The descriptor context</param>
    /// <param name="argumentName">The name of the argument</param>
    /// <returns>An instance of <see cref="DirectiveArgumentDescriptor "/></returns>
    public static DirectiveArgumentDescriptor New(
        IDescriptorContext context,
        string argumentName)
        => new(context, argumentName);

    /// <summary>
    /// Creates a new instance of <see cref="DirectiveArgumentDescriptor "/>
    /// </summary>
    /// <param name="context">The descriptor context</param>
    /// <param name="property">The property this argument is used for</param>
    /// <returns>An instance of <see cref="DirectiveArgumentDescriptor "/></returns>
    public static DirectiveArgumentDescriptor New(
        IDescriptorContext context,
        PropertyInfo property)
        => new(context, property);

    /// <summary>
    /// Creates a new instance of <see cref="DirectiveArgumentDescriptor "/>
    /// </summary>
    /// <param name="context">The descriptor context</param>
    /// <param name="definition">The definition of the argument</param>
    /// <returns>An instance of <see cref="DirectiveArgumentDescriptor "/></returns>
    public static DirectiveArgumentDescriptor From(
        IDescriptorContext context,
        DirectiveArgumentConfiguration definition)
        => new(context, definition);
}
