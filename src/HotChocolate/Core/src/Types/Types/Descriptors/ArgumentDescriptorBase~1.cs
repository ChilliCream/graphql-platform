using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Types.Helpers;

#nullable enable

namespace HotChocolate.Types.Descriptors;

/// <summary>
/// A fluent configuration API for GraphQL arguments.
/// </summary>
/// <typeparam name="T">The type of the <see cref="ArgumentConfiguration"/></typeparam>
public class ArgumentDescriptorBase<T> : DescriptorBase<T> where T : ArgumentConfiguration, new()
{
    /// <summary>
    ///  Creates a new instance of <see cref="ArgumentDescriptor"/>
    /// </summary>
    protected ArgumentDescriptorBase(IDescriptorContext context)
        : base(context)
    {
        Configuration = new T();
    }

    /// <inheritdoc />
    protected internal override T Configuration { get; protected set; }

    /// <inheritdoc cref="IArgumentDescriptor.Deprecated(string)"/>
    protected void Deprecated(string? reason)
    {
        if (string.IsNullOrEmpty(reason))
        {
            Deprecated();
        }
        else
        {
            Configuration.DeprecationReason = reason;
        }
    }

    /// <inheritdoc cref="IArgumentDescriptor.Deprecated()"/>
    protected void Deprecated()
    {
        Configuration.DeprecationReason = DirectiveNames.Deprecated.Arguments.DefaultReason;
    }

    /// <inheritdoc cref="IArgumentDescriptor.Description(string)"/>
    protected void Description(string value)
    {
        Configuration.Description = value;
    }

    /// <inheritdoc cref="IArgumentDescriptor.Type{TInputType}()"/>
    public void Type<TInputType>() where TInputType : IInputType
    {
        Type(typeof(TInputType));
    }

    /// <summary>
    /// Sets the type of the argument
    /// <example>
    /// <code lang="csharp">
    /// descriptor.Type(typeof(StringType));
    /// </code>
    /// Results in the following schema
    /// <code lang="graphql">
    /// type Query {
    ///     ships(name: String): [Ship!]!
    /// }
    /// </code>
    /// </example>
    /// </summary>
    public void Type(Type type)
    {
        var typeInfo = Context.TypeInspector.CreateTypeInfo(type);

        if (typeInfo.IsSchemaType && !typeInfo.IsInputType())
        {
            throw new ArgumentException(TypeResources.ArgumentDescriptor_InputTypeViolation);
        }

        Configuration.SetMoreSpecificType(typeInfo.GetExtendedType(), TypeContext.Input);
    }

    /// <inheritdoc cref="IArgumentDescriptor.Type{TInputType}(TInputType)"/>
    public void Type<TInputType>(TInputType inputType)
        where TInputType : class, IInputType
    {
        if (inputType is null)
        {
            throw new ArgumentNullException(nameof(inputType));
        }

        if (!inputType.IsInputType())
        {
            throw new ArgumentException(
                TypeResources.ArgumentDescriptor_InputTypeViolation,
                nameof(inputType));
        }

        Configuration.Type = new SchemaTypeReference(inputType);
    }

    /// <summary>
    /// Sets the type of the argument via a type reference
    /// <example>
    /// <code lang="csharp">
    /// definitions
    /// ITypeInspector inspector;
    /// ParameterInfo parameter;
    /// get  reference
    /// TypeReference reference = inspector.GetArgumentType(parameter)
    /// descriptor.Type(reference);
    /// </code>
    /// <p>
    /// Results in the following schema
    /// </p>
    /// <code lang="graphql">
    /// type Query {
    ///     ships(name: String): [Ship!]!
    /// }
    /// </code>
    /// </example>
    /// </summary>
    public void Type(TypeReference typeReference)
    {
        if (typeReference is null)
        {
            throw new ArgumentNullException(nameof(typeReference));
        }

        Configuration.Type = typeReference;
    }

    /// <inheritdoc cref="IArgumentDescriptor.Type(ITypeNode)"/>
    public void Type(ITypeNode typeNode)
    {
        if (typeNode is null)
        {
            throw new ArgumentNullException(nameof(typeNode));
        }

        Configuration.SetMoreSpecificType(typeNode, TypeContext.Input);
    }

    /// <inheritdoc cref="IArgumentDescriptor.DefaultValue(IValueNode)"/>
    public void DefaultValue(IValueNode? value)
    {
        Configuration.DefaultValue = value ?? NullValueNode.Default;
        Configuration.RuntimeDefaultValue = null;
    }

    /// <inheritdoc cref="IArgumentDescriptor.DefaultValue(object)"/>
    public void DefaultValue(object? value)
    {
        if (value is null)
        {
            Configuration.DefaultValue = NullValueNode.Default;
            Configuration.RuntimeDefaultValue = null;
        }
        else
        {
            var type = Context.TypeInspector.GetType(value.GetType());
            Configuration.SetMoreSpecificType(type, TypeContext.Input);
            Configuration.RuntimeDefaultValue = value;
            Configuration.DefaultValue = null;
        }
    }

    /// <inheritdoc cref="IArgumentDescriptor.Directive{T}(T)"/>
    public void Directive<TDirective>(TDirective directiveInstance) where TDirective : class
        => Configuration.AddDirective(directiveInstance, Context.TypeInspector);

    /// <inheritdoc cref="IArgumentDescriptor.Directive{T}()"/>
    public void Directive<TDirective>() where TDirective : class, new()
        => Configuration.AddDirective(new TDirective(), Context.TypeInspector);

    /// <inheritdoc cref="IArgumentDescriptor.Directive(string, ArgumentNode[])"/>
    public void Directive(string name, params ArgumentNode[] arguments)
        => Configuration.AddDirective(name, arguments);
}
