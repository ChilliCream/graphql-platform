using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types;

public interface IInputFieldDescriptor
    : IDescriptor<InputFieldDefinition>
    , IFluent
{
    /// <summary>
    /// Sets the name of the argument
    /// <example>
    /// <code lang="csharp">
    /// descriptor.Name("thisIsTheName");
    /// </code>
    /// Results in the following schema
    /// <code lang="graphql">
    /// type Query {
    ///     ships(thisIsTheName: String) [Ship!]!
    /// }
    /// </code>
    /// </example>
    /// </summary>
    IInputFieldDescriptor Name(string value);

    /// <summary>
    /// Marks the field as deprecated
    /// <remarks>
    /// The field must be nullable or have a default value. Otherwise the field cannot be deprecated
    /// </remarks>
    /// <example>
    /// <code lang="csharp">
    /// descriptor.Deprecated("The provided reason");
    /// </code>
    /// Results in the following schema
    /// <code lang="graphql">
    /// input Example {
    ///     field: String @deprecated(reason: "The provided reason")
    /// }
    /// </code>
    /// </example>
    /// </summary>
    IInputFieldDescriptor Deprecated(string reason);

    /// <summary>
    /// Marks the field as deprecated
    /// <remarks>
    /// The field must be nullable. Non-Nullable field cannot be deprecated
    /// </remarks>
    /// <example>
    /// <code lang="csharp">
    /// descriptor.Deprecated();
    /// </code>
    /// Results in the following schema
    /// <code lang="graphql">
    /// input Example {
    ///     field: String @deprecated(reason: "No longer supported.")
    /// }
    /// </code>
    /// </example>
    /// </summary>
    IInputFieldDescriptor Deprecated();

    /// <summary>
    /// Set the description of this argument
    /// <example>
    /// <code lang="csharp">
    /// descriptor.Description("Returns all ships");
    /// </code>
    /// Results in the following schema
    /// <code lang="graphql">
    /// input Example {
    ///     """
    ///     Returns all ships
    ///     """
    ///     field: String!
    /// }
    /// </code>
    /// </example>
    /// </summary>
    /// <param name="value">The description</param>
    IInputFieldDescriptor Description(string value);

    /// <summary>
    /// Sets the type of the argument
    /// <example>
    /// <code lang="csharp">
    /// descriptor.Type&lt;MyType>();
    /// </code>
    /// Results in the following schema
    /// <code lang="graphql">
    /// input Example {
    ///     field: MyType
    /// }
    /// </code>
    /// </example>
    /// </summary>
    /// <typeparam name="TInputType"></typeparam>
    IInputFieldDescriptor Type<TInputType>()
        where TInputType : IInputType;

    /// <summary>
    /// Sets the type of the argument
    /// <example>
    /// <code lang="csharp">
    /// descriptor.Type(new StringType("TheTypeName"));
    /// </code>
    /// Results in the following schema
    /// <code lang="graphql">
    /// input Example {
    ///     field: TheTypeName
    /// }
    /// </code>
    /// </example>
    /// </summary>
    /// <typeparam name="TInputType"></typeparam>
    IInputFieldDescriptor Type<TInputType>(TInputType inputType)
        where TInputType : class, IInputType;

    /// <summary>
    /// Sets the type of the argument
    /// <example>
    /// <code lang="csharp">
    /// descriptor.Type(new StringType("TheTypeName"));
    /// </code>
    /// Results in the following schema
    /// <code lang="graphql">
    /// input Example {
    ///     field: TheTypeName
    /// }
    /// </code>
    /// </example>
    /// </summary>
    IInputFieldDescriptor Type(ITypeNode typeNode);

    /// <summary>
    /// Sets the type of the argument
    /// <example>
    /// <code lang="csharp">
    /// descriptor.Type(typeof(MyType));
    /// </code>
    /// Results in the following schema
    /// <code lang="graphql">
    /// input Example {
    ///     field: MyType
    /// }
    /// </code>
    /// </example>
    /// </summary>
    IInputFieldDescriptor Type(Type type);

    /// <summary>
    /// Ignores the argument and does not add it to the schema
    /// </summary>
    /// <param name="ignore">Ignores the argument when <c>true</c></param>
    IInputFieldDescriptor Ignore(bool ignore = true);

    /// <summary>
    /// Sets the default value of this argument
    /// <example>
    /// <code lang="csharp">
    /// descriptor.DefaultValue(new StringValueNode("falcon"));
    /// </code>
    /// Results in the following schema
    /// <code lang="graphql">
    /// input Example {
    ///     field: String = "falcon"
    /// }
    /// </code>
    /// </example>
    /// </summary>
    /// <param name="value"></param>
    IInputFieldDescriptor DefaultValue(IValueNode value);

    /// <summary>
    /// Sets the default value of this argument
    /// <example>
    /// <code lang="csharp">
    /// descriptor.DefaultValue("falcon");
    /// </code>
    /// Results in the following schema
    /// <code lang="graphql">
    /// input Example {
    ///     field: String = "falcon"
    /// }
    /// </code>
    /// </example>
    /// </summary>
    /// <param name="value"></param>
    IInputFieldDescriptor DefaultValue(object value);

    /// <summary>
    /// Sets a directive on the argument
    /// <example>
    /// <code lang="csharp">
    /// descriptor.Directive(new MyDirective());
    /// </code>
    /// Results in the following schema
    /// <code lang="graphql">
    /// input Example {
    ///     field: String @myDirective
    /// }
    /// </code>
    /// </example>
    /// </summary>
    /// <param name="directiveInstance">
    /// The instance of the directive
    /// </param>
    /// <typeparam name="T">The type of the directive</typeparam>
    IInputFieldDescriptor Directive<T>(T directiveInstance)
        where T : class;

    /// <summary>
    /// Sets a directive on the argument
    /// <example>
    /// <code lang="csharp">
    /// descriptor.Directive&lt;MyDirective>();
    /// </code>
    /// Results in the following schema
    /// <code lang="graphql">
    /// input Example {
    ///     field: String @myDirective
    /// }
    /// </code>
    /// </example>
    /// </summary>
    /// <typeparam name="T">The type of the directive</typeparam>
    IInputFieldDescriptor Directive<T>()
        where T : class, new();

    /// <summary>
    /// Sets a directive on the argument
    /// <example>
    /// <code lang="csharp">
    /// descriptor.Directive("myDirective");
    /// </code>
    /// Results in the following schema
    /// <code lang="graphql">
    /// input Example {
    ///     field: String @myDirective
    /// }
    /// </code>
    /// </example>
    /// </summary>
    /// <param name="name">The name of the directive</param>
    /// <param name="arguments">The arguments of the directive</param>
    IInputFieldDescriptor Directive(
        string name,
        params ArgumentNode[] arguments);
}
