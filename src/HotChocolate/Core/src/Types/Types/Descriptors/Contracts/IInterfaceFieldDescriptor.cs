#nullable enable

using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types;

/// <summary>
/// A fluent configuration API for GraphQL interface type fields.
/// </summary>
public interface IInterfaceFieldDescriptor
    : IDescriptor<InterfaceFieldDefinition>
    , IFluent
{
    /// <summary>
    /// Defines the name of the <see cref="InterfaceField"/>.
    /// </summary>
    /// <param name="value">The interface field name.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="value"/> is <c>null</c> or
    /// <see cref="string.Empty"/>.
    /// </exception>
    IInterfaceFieldDescriptor Name(string value);

    /// <summary>
    /// Adds explanatory text to the <see cref="InterfaceField"/>
    /// that can be accessed via introspection.
    /// </summary>
    /// <param name="value">The object field description.</param>
    IInterfaceFieldDescriptor Description(string? value);

    /// <summary>
    /// Deprecates the object field.
    /// </summary>
    /// <param name="reason">The reason why this field is deprecated.</param>
    IInterfaceFieldDescriptor Deprecated(string? reason);

    /// <summary>
    /// Deprecates the object field.
    /// </summary>
    IInterfaceFieldDescriptor Deprecated();

    /// <summary>
    /// Defines the type of the object field.
    /// </summary>
    /// <typeparam name="TOutputType">
    /// The type.
    /// </typeparam>
    IInterfaceFieldDescriptor Type<TOutputType>()
        where TOutputType : IOutputType;

    /// <summary>
    /// Defines the type of the object field.
    /// </summary>
    /// <typeparam name="TOutputType">
    /// The type.
    /// </typeparam>
    /// <param name="outputType">
    /// The output type instance.
    /// </param>
    IInterfaceFieldDescriptor Type<TOutputType>(TOutputType outputType)
        where TOutputType : class, IOutputType;

    /// <summary>
    /// Defines the type of the object field.
    /// </summary>
    IInterfaceFieldDescriptor Type(ITypeNode type);

    /// <summary>
    /// Defines the type of the object field.
    /// </summary>
    IInterfaceFieldDescriptor Type(Type type);

    /// <summary>
    /// Defines weather the resolver pipeline will return <see cref="IAsyncEnumerable{T}"/>
    /// as its result.
    /// </summary>
    IInterfaceFieldDescriptor StreamResult(bool hasStreamResult = true);

    /// <summary>
    /// Defines a field argument.
    /// </summary>
    /// <param name="argumentName">
    /// The field argument name.
    /// </param>
    /// <param name="argumentDescriptor">
    /// The argument descriptor to specify the argument configuration.
    /// </param>
    IInterfaceFieldDescriptor Argument(
        string argumentName,
        Action<IArgumentDescriptor> argumentDescriptor);

    /// <summary>
    /// Ignores the given field for the schema creation.
    /// This field will not be included into the GraphQL schema.
    /// </summary>
    /// <param name="ignore">
    /// The value specifying if this field shall be ignored by the type initialization.
    /// </param>
    IInterfaceFieldDescriptor Ignore(bool ignore = true);

    /// <summary>
    /// Adds a resolver to the field. A resolver is a method that resolves the value for a
    /// field. The resolver can access parent object, arguments, services and more through the
    /// <see cref="IResolverContext"/>.
    /// </summary>
    /// <param name="fieldResolver">The resolver of the field</param>
    /// <example>
    /// Resolver accessing the parent
    /// <code>
    /// <![CDATA[
    /// descriptor
    ///     .Field(x => x.Foo)
    ///     .Resolve(context => context.Parent<Example>().Foo);
    /// ]]>
    /// </code>
    /// Resolver with static value
    /// <code>
    /// <![CDATA[
    /// descriptor
    ///     .Field(x => x.Foo)
    ///     .Resolve("Static Value");
    /// ]]>
    /// </code>
    /// Resolver accessing service
    /// <code>
    /// <![CDATA[
    /// descriptor
    ///     .Field(x => x.Foo)
    ///     .Resolve(context => context.Service<ISomeService>().GetFoo());
    /// ]]>
    /// </code>
    /// Resolver accessing argument
    /// <code>
    /// <![CDATA[
    /// descriptor
    ///     .Field(x => x.Foo)
    ///     .Argument("arg1", x => x.Type<StringType>())
    ///     .Resolve(context => context.ArgumentValue<string>("arg1"));
    /// ]]>
    /// </code>
    /// </example>
    /// <returns></returns>
    IInterfaceFieldDescriptor Resolve(FieldResolverDelegate fieldResolver);

    /// <summary>
    /// Adds a resolver to the field. A resolver is a method that resolves the value for a
    /// field. The resolver can access parent object, arguments, services and more through the
    /// <see cref="IResolverContext"/>.
    /// </summary>
    /// <param name="fieldResolver">The resolver of the field</param>
    /// <param name="resultType">The result type of the resolver</param>
    /// <example>
    /// Resolver accessing the parent
    /// <code>
    /// <![CDATA[
    /// descriptor
    ///     .Field(x => x.Foo)
    ///     .Resolve(context => context.Parent<Example>().Foo, typeof(string));
    /// ]]>
    /// </code>
    /// </example>
    /// <returns></returns>
    IInterfaceFieldDescriptor Resolve(
        FieldResolverDelegate fieldResolver,
        Type? resultType);

    /// <summary>
    /// Adds a resolver based on a method to the field.
    /// A resolver is a method that resolves the value for a
    /// field. The resolver can access parent object, arguments, services and more through the
    /// <see cref="IResolverContext"/>.
    /// </summary>
    /// <param name="propertyOrMethod">The resolver of the field</param>
    /// <example>
    /// Given the following resolvers class
    /// <code>
    /// <![CDATA[
    /// private sealed class Resolvers
    /// {
    ///    public ValueTask<string> GetFoo(
    ///        [Service] IFooService service,
    ///        CancellationToken cancellationToken) =>
    ///        service.GetFooAsync(cancellationToken);
    /// }
    /// ]]>
    /// </code>
    /// The GetFoo method can be mapped like:
    /// <code>
    /// <![CDATA[
    /// descriptor
    ///     .Field(x => x.Foo)
    ///     .ResolveWith<Resolvers>(t => t.GetFoo(default!, default));
    /// ]]>
    /// </code>
    /// </example>
    /// <returns></returns>
    IInterfaceFieldDescriptor ResolveWith<TResolver>(
        Expression<Func<TResolver, object>> propertyOrMethod);

    /// <summary>
    /// Adds a resolver based on a method to the field.
    /// A resolver is a method that resolves the value for a
    /// field. The resolver can access parent object, arguments, services and more through the
    /// <see cref="IResolverContext"/>.
    /// </summary>
    /// <param name="propertyOrMethod">The resolver of the field</param>
    /// <example>
    /// Given the following resolvers class
    /// <code>
    /// <![CDATA[
    /// private sealed class Resolvers
    /// {
    ///    public ValueTask<string> GetFoo(
    ///        [Service] IFooService service,
    ///        CancellationToken cancellationToken) =>
    ///        service.GetFooAsync(cancellationToken);
    /// }
    /// ]]>
    /// </code>
    /// The GetFoo method can be mapped like:
    /// <code>
    /// <![CDATA[
    /// descriptor
    ///     .Field(x => x.Foo)
    ///     .ResolveWith<Resolvers>(typeof(Resolvers).GetMethod("GetFoo"));
    /// ]]>
    /// </code>
    /// </example>
    /// <returns></returns>
    IInterfaceFieldDescriptor ResolveWith(MemberInfo propertyOrMethod);

    /// <summary>
    /// Registers a middleware on the field. The middleware is integrated in the resolver
    /// pipeline and is executed before the resolver itself
    /// </summary>
    /// <param name="middleware">The middleware</param>
    /// <example>
    /// <code>
    /// <![CDATA[
    /// descriptor
    ///     .Field(x => x.Foo)
    ///     .Use(next => async context =>
    ///     {
    ///         // before the resolver
    ///         await next(context);
    ///         // after the resolver
    ///     });
    /// ]]>
    /// </code>
    /// </example>
    /// <returns>The descriptor</returns>
    IInterfaceFieldDescriptor Use(FieldMiddleware middleware);

    /// <summary>
    /// Registers a directive on the field
    /// <example>
    /// <code lang="csharp">
    /// descriptor.Directive(new MyDirective());
    /// </code>
    /// Results in the following schema
    /// <code lang="graphql">
    /// type Port {
    ///     ships(name: String): [Ship!]! @myDirective
    /// }
    /// </code>
    /// </example>
    /// </summary>
    /// <param name="directiveInstance">The instance of the directive</param>
    /// <typeparam name="T">The type of the directive</typeparam>
    /// <returns>The descriptor</returns>
    IInterfaceFieldDescriptor Directive<T>(T directiveInstance)
        where T : class;

    /// <summary>
    /// Registers a directive on the field
    /// <example>
    /// <code lang="csharp">
    /// descriptor.Directive&lt;MyDirective>();
    /// </code>
    /// Results in the following schema
    /// <code lang="graphql">
    /// type Port {
    ///     ships(name: String): [Ship!]! @myDirective
    /// }
    /// </code>
    /// </example>
    /// </summary>
    /// <typeparam name="T">The type of the directive</typeparam>
    /// <returns>The descriptor</returns>
    IInterfaceFieldDescriptor Directive<T>()
        where T : class, new();

    /// <summary>
    /// Registers a directive on the field
    /// <example>
    /// <code lang="csharp">
    /// descriptor.Directive("myDirective");
    /// </code>
    /// Results in the following schema
    /// <code lang="graphql">
    /// type Port {
    ///     ships(name: String): [Ship!]! @myDirective
    /// }
    /// </code>
    /// </example>
    /// </summary>
    /// <param name="name">The name of the directive</param>
    /// <param name="arguments">The arguments of the directive</param>
    /// <returns>The descriptor</returns>
    IInterfaceFieldDescriptor Directive(
        string name,
        params ArgumentNode[] arguments);
}
