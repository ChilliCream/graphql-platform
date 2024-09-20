using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types;

public interface IArgumentDescriptor
    : IDescriptor<ArgumentDefinition>
    , IFluent
{
    /// <summary>
    /// Marks the argument as deprecated
    /// <remarks>
    /// The argument must be nullable. Non-Nullable arguments cannot be deprecated
    /// </remarks>
    /// <example>
    /// <code lang="csharp">
    /// descriptor.Deprecated("The provided reason");
    /// </code>
    /// Results in the following schema
    /// <code lang="graphql">
    /// type Query {
    ///     ships(name: String @deprecated(reason: "The provided reason"): [Ship!]!
    /// }
    /// </code>
    /// </example>
    /// </summary>
    IArgumentDescriptor Deprecated(string reason);

    /// <summary>
    /// Marks the argument as deprecated
    /// <remarks>
    /// The argument must be nullable or have a default value. Otherwise the argument cannot be deprecated
    /// </remarks>
    /// <example>
    /// <code lang="csharp">
    /// descriptor.Deprecated();
    /// </code>
    /// Results in the following schema
    /// <code lang="graphql">
    /// type Query {
    ///     ships(name: String @deprecated(reason: "No longer supported.")): [Ship!]!
    /// }
    /// </code>
    /// </example>
    /// </summary>
    IArgumentDescriptor Deprecated();

    /// <summary>
    /// Set the description of this argument
    /// <example>
    /// <code lang="csharp">
    /// descriptor.Description("Returns all ships");
    /// </code>
    /// Results in the following schema
    /// <code lang="graphql">
    /// type Query {
    ///     """
    ///     Returns all ships
    ///     """
    ///     ships(name: String): [Ship!]!
    /// }
    /// </code>
    /// </example>
    /// </summary>
    /// <param name="value">The description</param>
    IArgumentDescriptor Description(string value);

    /// <summary>
    /// Sets the type of the argument
    /// <example>
    /// <code lang="csharp">
    /// descriptor.Type&lt;StringType>();
    /// </code>
    /// Results in the following schema
    /// <code lang="graphql">
    /// type Query {
    ///     ships(name: String): [Ship!]!
    /// }
    /// </code>
    /// </example>
    /// </summary>
    /// <typeparam name="TInputType"></typeparam>
    IArgumentDescriptor Type<TInputType>()
        where TInputType : IInputType;

    /// <summary>
    /// Sets the type of the argument
    /// <example>
    /// <code lang="csharp">
    /// descriptor.Type(new StringType("ShipName"));
    /// </code>
    /// Results in the following schema
    /// <code lang="graphql">
    /// type Query {
    ///     ships(name: ShipName): [Ship!]!
    /// }
    /// </code>
    /// </example>
    /// </summary>
    IArgumentDescriptor Type<TInputType>(TInputType inputType)
        where TInputType : class, IInputType;

    /// <summary>
    /// Sets the type of the argument
    /// <example>
    /// <code lang="csharp">
    /// descriptor.Type(new NamedTypeNode("String"));
    /// </code>
    /// Results in the following schema
    /// <code lang="graphql">
    /// type Query {
    ///     ships(name: String): [Ship!]!
    /// }
    /// </code>
    /// </example>
    /// </summary>
    IArgumentDescriptor Type(ITypeNode typeNode);

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
    IArgumentDescriptor Type(Type type);

    /// <summary>
    /// Sets the default value of this argument
    /// <example>
    /// <code lang="csharp">
    /// descriptor.DefaultValue(new StringValueNode("falcon"));
    /// </code>
    /// Results in the following schema
    /// <code lang="graphql">
    /// type Query {
    ///     ships(name: String = "falcon"): [Ship!]!
    /// }
    /// </code>
    /// </example>
    /// </summary>
    /// <param name="value"></param>
    IArgumentDescriptor DefaultValue(IValueNode value);

    /// <summary>
    /// Sets the default value of this argument
    /// <example>
    /// <code lang="csharp">
    /// descriptor.DefaultValue("falcon");
    /// </code>
    /// Results in the following schema
    /// <code lang="graphql">
    /// type Query {
    ///     ships(name: String = "falcon"): [Ship!]!
    /// }
    /// </code>
    /// </example>
    /// </summary>
    /// <param name="value"></param>
    IArgumentDescriptor DefaultValue(object value);

    /// <summary>
    /// Sets a directive on the argument
    /// <example>
    /// <code lang="csharp">
    /// descriptor.Directive(new MyDirective());
    /// </code>
    /// Results in the following schema
    /// <code lang="graphql">
    /// type Query {
    ///     ships(name: String @myDirective): [Ship!]!
    /// }
    /// </code>
    /// </example>
    /// </summary>
    /// <param name="directiveInstance">
    /// The instance of the directive
    /// </param>
    /// <typeparam name="T">The type of the directive</typeparam>
    IArgumentDescriptor Directive<T>(T directiveInstance)
        where T : class;

    /// <summary>
    /// Sets a directive on the argument
    /// <example>
    /// <code lang="csharp">
    /// descriptor.Directive&lt;MyDirective>();
    /// </code>
    /// Results in the following schema
    /// <code lang="graphql">
    /// type Query {
    ///     ships(name: String @myDirective): [Ship!]!
    /// }
    /// </code>
    /// </example>
    /// </summary>
    /// <typeparam name="T">The type of the directive</typeparam>
    IArgumentDescriptor Directive<T>()
        where T : class, new();

    /// <summary>
    /// Sets a directive on the argument
    /// <example>
    /// <code lang="csharp">
    /// descriptor.Directive("myDirective");
    /// </code>
    /// Results in the following schema
    /// <code lang="graphql">
    /// type Query {
    ///     ships(name: String @myDirective): [Ship!]!
    /// }
    /// </code>
    /// </example>
    /// </summary>
    /// <param name="name">The name of the directive</param>
    /// <param name="arguments">The arguments of the directive</param>
    IArgumentDescriptor Directive(string name, params ArgumentNode[] arguments);
}
