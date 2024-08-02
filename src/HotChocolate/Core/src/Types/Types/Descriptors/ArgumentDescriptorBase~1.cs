using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Types.Helpers;

#nullable enable

namespace HotChocolate.Types.Descriptors;

/// <summary>
/// A fluent configuration API for GraphQL arguments.
/// </summary>
/// <typeparam name="T">The type of the <see cref="ArgumentDefinition"/></typeparam>
public class ArgumentDescriptorBase<T> : DescriptorBase<T> where T : ArgumentDefinition, new()
{
    /// <summary>
    ///  Creates a new instance of <see cref="ArgumentDescriptor"/>
    /// </summary>
    protected ArgumentDescriptorBase(IDescriptorContext context)
        : base(context)
    {
        Definition = new T();
    }

    /// <inheritdoc />
    protected internal override T Definition { get; protected set; }

    /// <inheritdoc cref="IArgumentDescriptor.Deprecated(string)"/>
    protected void Deprecated(string? reason)
    {
        if (string.IsNullOrEmpty(reason))
        {
            Deprecated();
        }
        else
        {
            Definition.DeprecationReason = reason;
        }
    }

    /// <inheritdoc cref="IArgumentDescriptor.Deprecated()"/>
    protected void Deprecated()
    {
        Definition.DeprecationReason = WellKnownDirectives.DeprecationDefaultReason;
    }

    /// <inheritdoc cref="IArgumentDescriptor.Description(string)"/>
    protected void Description(string value)
    {
        Definition.Description = value;
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

        Definition.SetMoreSpecificType(typeInfo.GetExtendedType(), TypeContext.Input);
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

        Definition.Type = new SchemaTypeReference(inputType);
    }

    /// <summary>
    /// Sets the type of the argument via a type reference
    /// <example>
    /// <code lang="csharp">
    /// // definitions
    /// ITypeInspector inspector;
    /// ParameterInfo parameter;
    /// // get  reference
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

        Definition.Type = typeReference;
    }

    /// <inheritdoc cref="IArgumentDescriptor.Type(ITypeNode)"/>
    public void Type(ITypeNode typeNode)
    {
        if (typeNode is null)
        {
            throw new ArgumentNullException(nameof(typeNode));
        }

        Definition.SetMoreSpecificType(typeNode, TypeContext.Input);
    }

    /// <inheritdoc cref="IArgumentDescriptor.DefaultValue(IValueNode)"/>
    public void DefaultValue(IValueNode? value)
    {
        Definition.DefaultValue = value ?? NullValueNode.Default;
        Definition.RuntimeDefaultValue = null;
    }

    /// <inheritdoc cref="IArgumentDescriptor.DefaultValue(object)"/>
    public void DefaultValue(object? value)
    {
        if (value is null)
        {
            Definition.DefaultValue = NullValueNode.Default;
            Definition.RuntimeDefaultValue = null;
        }
        else
        {
            var type = Context.TypeInspector.GetType(value.GetType());
            Definition.SetMoreSpecificType(type, TypeContext.Input);
            Definition.RuntimeDefaultValue = value;
            Definition.DefaultValue = null;
        }
    }

    /// <inheritdoc cref="IArgumentDescriptor.Directive{T}(T)"/>
    public void Directive<TDirective>(TDirective directiveInstance) where TDirective : class
        => Definition.AddDirective(directiveInstance, Context.TypeInspector);

    /// <inheritdoc cref="IArgumentDescriptor.Directive{T}()"/>
    public void Directive<TDirective>() where TDirective : class, new()
        => Definition.AddDirective(new TDirective(), Context.TypeInspector);

    /// <inheritdoc cref="IArgumentDescriptor.Directive(string, ArgumentNode[])"/>
    public void Directive(string name, params ArgumentNode[] arguments)
        => Definition.AddDirective(name, arguments);
}
