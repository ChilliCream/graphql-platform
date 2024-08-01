using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Definitions;

#nullable enable

namespace HotChocolate.Types;

/// <summary>
/// A fluent configuration API for GraphQL object types.
/// </summary>
public interface IObjectTypeDescriptor
    : IDescriptor<ObjectTypeDefinition>
    , IFluent
{
    /// <summary>
    /// Defines the name of the <see cref="ObjectType"/>.
    /// </summary>
    /// <param name="value">The object type name.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="value"/> is <c>null</c> or
    /// <see cref="string.Empty"/>.
    /// </exception>
    IObjectTypeDescriptor Name(string value);

    /// <summary>
    /// Adds explanatory text to the <see cref="ObjectType"/>
    /// that can be accessed via introspection.
    /// </summary>
    /// <param name="value">The object type description.</param>
    IObjectTypeDescriptor Description(string? value);

    /// <summary>
    /// Specifies an interface that is implemented by the
    /// <see cref="ObjectType"/>.
    /// </summary>
    /// <typeparam name="T">The interface type.</typeparam>
    IObjectTypeDescriptor Implements<T>()
        where T : InterfaceType;

    /// <summary>
    /// Specifies an interface that is implemented by the
    /// <see cref="ObjectType"/>.
    /// </summary>
    /// <typeparam name="T">The interface type.</typeparam>
    IObjectTypeDescriptor Implements<T>(T type)
        where T : InterfaceType;

    /// <summary>
    /// Specifies an interface that is implemented by the
    /// <see cref="ObjectType"/>.
    /// </summary>
    /// <param name="type">
    /// A syntax node representing an interface type.
    /// </param>
    IObjectTypeDescriptor Implements(NamedTypeNode type);

    /// <summary>
    /// Specifies a delegate that can determine if a resolver result
    /// represents an object instance of this <see cref="ObjectType"/>.
    /// </summary>
    /// <param name="isOfType">
    /// The delegate that provides the IsInstanceOfType functionality.
    /// </param>
    IObjectTypeDescriptor IsOfType(IsOfType? isOfType);

    /// <summary>
    /// Specifies an object type field.
    /// </summary>
    /// <param name="name">
    /// The name that the field shall have.
    /// </param>
    IObjectFieldDescriptor Field(string name);

    /// <summary>
    /// Specifies an object type field which is bound to a resolver type.
    /// </summary>
    /// <param name="propertyOrMethod">
    /// An expression selecting a property or method of
    /// <typeparamref name="TResolver"/>.
    /// The resolver type containing the property or method.
    /// </param>
    IObjectFieldDescriptor Field<TResolver>(
        Expression<Func<TResolver, object?>> propertyOrMethod);

    /// <summary>
    /// Specifies an object type field which is bound to a resolver type.
    /// </summary>
    /// <param name="propertyOrMethod">
    /// The member that shall be used as a field.
    /// </param>
    IObjectFieldDescriptor Field(MemberInfo propertyOrMethod);

    /// <summary>
    /// Sets a directive on the object type
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
    IObjectTypeDescriptor Directive<T>(T directiveInstance)
        where T : class;

    /// <summary>
    /// Sets a directive on the object type
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
    IObjectTypeDescriptor Directive<T>()
        where T : class, new();

    /// <summary>
    /// Sets a directive on the object type
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
    IObjectTypeDescriptor Directive(string name, params ArgumentNode[] arguments);

    /// <summary>
    /// If configuring a type extension this is the type that shall be extended.
    /// </summary>
    /// <param name="extendsType">
    /// The type to extend.
    /// </param>
    IObjectTypeDescriptor ExtendsType(Type extendsType);

    /// <summary>
    /// If configuring a type extension this is the type that shall be extended.
    /// </summary>
    /// <typeparam name="T">The type to extend.</typeparam>
    IObjectTypeDescriptor ExtendsType<T>();
}
