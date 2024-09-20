// ReSharper disable VirtualMemberCallInConstructor

using System.Reflection;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types.Descriptors;

/// <summary>
/// A fluent configuration API for GraphQL arguments.
/// </summary>
public class ArgumentDescriptor
    : ArgumentDescriptorBase<ArgumentDefinition>
    , IArgumentDescriptor
{
    /// <summary>
    ///  Creates a new instance of <see cref="ArgumentDescriptor"/>
    /// </summary>
    protected internal ArgumentDescriptor(
        IDescriptorContext context,
        string argumentName)
        : base(context)
    {
        Definition.Name = argumentName;
    }

    /// <summary>
    ///  Creates a new instance of <see cref="ArgumentDescriptor"/>
    /// </summary>
    protected internal ArgumentDescriptor(
        IDescriptorContext context,
        string argumentName,
        Type argumentType)
        : this(context, argumentName)
    {
        if (argumentType is null)
        {
            throw new ArgumentNullException(nameof(argumentType));
        }

        Definition.Type = context.TypeInspector.GetTypeRef(argumentType, TypeContext.Input);
    }

    /// <summary>
    ///  Creates a new instance of <see cref="ArgumentDescriptor"/>
    /// </summary>
    protected internal ArgumentDescriptor(
        IDescriptorContext context,
        ParameterInfo parameter)
        : base(context)
    {
        Definition.Name = context.Naming.GetArgumentName(parameter);
        Definition.Description = context.Naming.GetArgumentDescription(parameter);
        Definition.Type = context.TypeInspector.GetArgumentTypeRef(parameter);
        Definition.Parameter = parameter;

        if (context.TypeInspector.TryGetDefaultValue(parameter, out var defaultValue))
        {
            Definition.RuntimeDefaultValue = defaultValue;
        }

        if (context.Naming.IsDeprecated(parameter, out var reason))
        {
            Deprecated(reason);
        }
    }

    /// <summary>
    ///  Creates a new instance of <see cref="ArgumentDescriptor"/>
    /// </summary>
    protected internal ArgumentDescriptor(
        IDescriptorContext context,
        ArgumentDefinition definition)
        : base(context)
    {
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
    }

    /// <inheritdoc />
    protected override void OnCreateDefinition(ArgumentDefinition definition)
    {
        Context.Descriptors.Push(this);

        if (Definition is { AttributesAreApplied: false, Parameter: not null, })
        {
            Context.TypeInspector.ApplyAttributes(
                Context,
                this,
                Definition.Parameter);
            Definition.AttributesAreApplied = true;
        }

        base.OnCreateDefinition(definition);

        Context.Descriptors.Pop();
    }

    /// <inheritdoc />
    public new IArgumentDescriptor Deprecated(string reason)
    {
        base.Deprecated(reason);
        return this;
    }

    /// <inheritdoc />
    public new IArgumentDescriptor Deprecated()
    {
        base.Deprecated();
        return this;
    }

    /// <inheritdoc />
    public new IArgumentDescriptor Description(string value)
    {
        base.Description(value);
        return this;
    }

    /// <inheritdoc />
    public new IArgumentDescriptor Type<TInputType>()
        where TInputType : IInputType
    {
        base.Type<TInputType>();
        return this;
    }

    /// <inheritdoc />
    public new IArgumentDescriptor Type<TInputType>(TInputType inputType)
        where TInputType : class, IInputType
    {
        base.Type(inputType);
        return this;
    }

    /// <inheritdoc />
    public new IArgumentDescriptor Type(ITypeNode typeNode)
    {
        base.Type(typeNode);
        return this;
    }

    /// <inheritdoc />
    public new IArgumentDescriptor Type(Type type)
    {
        base.Type(type);
        return this;
    }

    /// <inheritdoc />
    public new IArgumentDescriptor DefaultValue(IValueNode value)
    {
        base.DefaultValue(value);
        return this;
    }

    /// <inheritdoc />
    public new IArgumentDescriptor DefaultValue(object value)
    {
        base.DefaultValue(value);
        return this;
    }

    /// <inheritdoc />
    public new IArgumentDescriptor Directive<TDirective>(TDirective directiveInstance)
        where TDirective : class
    {
        base.Directive(directiveInstance);
        return this;
    }

    /// <inheritdoc />
    public new IArgumentDescriptor Directive<TDirective>()
        where TDirective : class, new()
    {
        base.Directive<TDirective>();
        return this;
    }

    /// <inheritdoc />
    public new IArgumentDescriptor Directive(
        string name,
        params ArgumentNode[] arguments)
    {
        base.Directive(name, arguments);
        return this;
    }

    /// <summary>
    /// Creates a new instance of <see cref="ArgumentDescriptor"/>
    /// </summary>
    /// <param name="context">The descriptor context</param>
    /// <param name="argumentName">The name of the argument</param>
    /// <returns>An instance of <see cref="ArgumentDescriptor"/></returns>
    public static ArgumentDescriptor New(
        IDescriptorContext context,
        string argumentName)
        => new(context, argumentName);

    /// <summary>
    /// Creates a new instance of <see cref="ArgumentDescriptor"/>
    /// </summary>
    /// <param name="context">The descriptor context</param>
    /// <param name="argumentName">The name of the argument</param>
    /// <param name="argumentType">The type of the argument</param>
    /// <returns>An instance of <see cref="ArgumentDescriptor"/></returns>
    public static ArgumentDescriptor New(
        IDescriptorContext context,
        string argumentName,
        Type argumentType) =>
        new(context, argumentName, argumentType);

    /// <summary>
    /// Creates a new instance of <see cref="ArgumentDescriptor"/>
    /// </summary>
    /// <param name="context">The descriptor context</param>
    /// <param name="parameter">The parameter this argument is used for</param>
    /// <returns>An instance of <see cref="ArgumentDescriptor"/></returns>
    public static ArgumentDescriptor New(
        IDescriptorContext context,
        ParameterInfo parameter) =>
        new(context, parameter);

    /// <summary>
    /// Creates a new instance of <see cref="ArgumentDescriptor"/>
    /// </summary>
    /// <param name="context">The descriptor context</param>
    /// <param name="argumentDefinition">The definition of the argument</param>
    /// <returns>An instance of <see cref="ArgumentDescriptor"/></returns>
    public static ArgumentDescriptor From(
        IDescriptorContext context,
        ArgumentDefinition argumentDefinition) =>
        new(context, argumentDefinition);
}
