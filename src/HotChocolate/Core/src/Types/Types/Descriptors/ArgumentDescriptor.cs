// ReSharper disable VirtualMemberCallInConstructor

using System.Reflection;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Configurations;

namespace HotChocolate.Types.Descriptors;

/// <summary>
/// A fluent configuration API for GraphQL arguments.
/// </summary>
public class ArgumentDescriptor
    : ArgumentDescriptorBase<ArgumentConfiguration>
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
        Configuration.Name = argumentName;
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
        ArgumentNullException.ThrowIfNull(argumentType);

        Configuration.Type = context.TypeInspector.GetTypeRef(argumentType, TypeContext.Input);
    }

    /// <summary>
    ///  Creates a new instance of <see cref="ArgumentDescriptor"/>
    /// </summary>
    protected internal ArgumentDescriptor(
        IDescriptorContext context,
        ParameterInfo parameter)
        : base(context)
    {
        Configuration.Name = context.Naming.GetArgumentName(parameter);
        Configuration.Description = context.Naming.GetArgumentDescription(parameter);
        Configuration.Type = context.TypeInspector.GetArgumentTypeRef(parameter);
        Configuration.Parameter = parameter;

        if (context.TypeInspector.TryGetDefaultValue(parameter, out var defaultValue))
        {
            Configuration.RuntimeDefaultValue = defaultValue;
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
        ArgumentConfiguration definition)
        : base(context)
    {
        Configuration = definition ?? throw new ArgumentNullException(nameof(definition));
    }

    /// <inheritdoc />
    protected override void OnCreateConfiguration(ArgumentConfiguration definition)
    {
        Context.Descriptors.Push(this);

        if (Configuration is { AttributesAreApplied: false, Parameter: not null })
        {
            Context.TypeInspector.ApplyAttributes(
                Context,
                this,
                Configuration.Parameter);
            Configuration.AttributesAreApplied = true;
        }

        base.OnCreateConfiguration(definition);

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
        ArgumentConfiguration argumentDefinition) =>
        new(context, argumentDefinition);
}
