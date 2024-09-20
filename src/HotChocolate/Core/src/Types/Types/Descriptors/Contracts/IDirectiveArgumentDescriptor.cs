using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types;

public interface IDirectiveArgumentDescriptor
    : IDescriptor<DirectiveArgumentDefinition>
    , IFluent
{
    /// <inheritdoc cref="IArgumentDescriptor.Deprecated(string)"/>
    IDirectiveArgumentDescriptor  Deprecated(string reason);

    /// <inheritdoc cref="IArgumentDescriptor.Deprecated()"/>
    IDirectiveArgumentDescriptor  Deprecated();

    /// <summary>
    /// Sets the name of the argument
    /// <example>
    /// <code lang="csharp">
    /// descriptor.Name("thisIsTheName");
    /// </code>
    /// Results in the following schema
    /// <code lang="graphql">
    /// type Query {
    ///     ships(name: String @myDirective(thisIsTheName: "&lt;----"): [Ship!]!
    /// }
    /// </code>
    /// </example>
    /// </summary>
    IDirectiveArgumentDescriptor Name(string value);

    /// <inheritdoc cref="IArgumentDescriptor.Description(string)"/>
    IDirectiveArgumentDescriptor Description(string value);

    /// <inheritdoc cref="IArgumentDescriptor.Type{TInputType}()"/>
    IDirectiveArgumentDescriptor Type<TInputType>()
        where TInputType : IInputType;

    /// <inheritdoc cref="IArgumentDescriptor.Type{TInputType}(TInputType)"/>
    IDirectiveArgumentDescriptor Type<TInputType>(TInputType inputType)
        where TInputType : class, IInputType;

    /// <inheritdoc cref="IArgumentDescriptor.Type(ITypeNode)"/>
    IDirectiveArgumentDescriptor Type(ITypeNode typeNode);

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
    IDirectiveArgumentDescriptor Type(Type type);

    /// <inheritdoc cref="IArgumentDescriptor.DefaultValue(IValueNode)"/>
    IDirectiveArgumentDescriptor DefaultValue(IValueNode value);

    /// <inheritdoc cref="IArgumentDescriptor.DefaultValue(object)"/>
    IDirectiveArgumentDescriptor DefaultValue(object value);

    /// <summary>
    /// Ignores the argument and does not add it to the schema
    /// </summary>
    /// <param name="ignore">Ignores the argument when <c>true</c></param>
    IDirectiveArgumentDescriptor Ignore(bool ignore = true);
}
