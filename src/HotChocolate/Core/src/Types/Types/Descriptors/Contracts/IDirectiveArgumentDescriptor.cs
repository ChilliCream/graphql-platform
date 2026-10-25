#nullable disable

using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Configurations;

namespace HotChocolate.Types;

public interface IDirectiveArgumentDescriptor
    : IDescriptor<DirectiveArgumentConfiguration>
    , IFluent
{
    /// <inheritdoc cref="IArgumentDescriptor.Deprecated(string)"/>
    IDirectiveArgumentDescriptor Deprecated(string reason);

    /// <inheritdoc cref="IArgumentDescriptor.Deprecated()"/>
    IDirectiveArgumentDescriptor Deprecated();

    /// <summary>
    /// Sets a directive on the argument
    /// <example>
    /// <code lang="csharp">
    /// descriptor.Directive(new MyDirective());
    /// </code>
    /// Results in the following schema
    /// <code lang="graphql">
    /// directive @example(arg: Int @myDirective) on FIELD
    /// </code>
    /// </example>
    /// </summary>
    /// <param name="directiveInstance">
    /// The instance of the directive
    /// </param>
    /// <typeparam name="T">The type of the directive</typeparam>
    IDirectiveArgumentDescriptor Directive<T>(T directiveInstance)
        where T : class;

    /// <summary>
    /// Sets a directive on the argument
    /// <example>
    /// <code lang="csharp">
    /// descriptor.Directive&lt;MyDirective>();
    /// </code>
    /// Results in the following schema
    /// <code lang="graphql">
    /// directive @example(arg: Int @myDirective) on FIELD
    /// </code>
    /// </example>
    /// </summary>
    /// <typeparam name="T">The type of the directive</typeparam>
    IDirectiveArgumentDescriptor Directive<T>()
        where T : class, new();

    /// <summary>
    /// Sets a directive on the argument
    /// <example>
    /// <code lang="csharp">
    /// descriptor.Directive("myDirective");
    /// </code>
    /// Results in the following schema
    /// <code lang="graphql">
    /// directive @example(arg: Int @myDirective) on FIELD
    /// </code>
    /// </example>
    /// </summary>
    /// <param name="name">The name of the directive</param>
    /// <param name="arguments">The arguments of the directive</param>
    IDirectiveArgumentDescriptor Directive(string name, params ArgumentNode[] arguments);

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
