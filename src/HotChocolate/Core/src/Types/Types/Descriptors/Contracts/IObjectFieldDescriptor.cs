using System;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors.Definitions;

#nullable enable

namespace HotChocolate.Types
{
    /// <summary>
    /// A fluent configuration API for GraphQL object type fields.
    /// </summary>
    public interface IObjectFieldDescriptor
        : IDescriptor<ObjectFieldDefinition>
        , IFluent
    {
        /// <summary>
        /// Associates the specified <paramref name="fieldDefinition"/>
        /// with the <see cref="ObjectField"/>.
        /// </summary>
        /// <param name="fieldDefinition">
        /// The <see cref="FieldDefinitionNode"/> of a parsed schema.
        /// </param>
        IObjectFieldDescriptor SyntaxNode(
            FieldDefinitionNode? fieldDefinition);

        /// <summary>
        /// Defines the name of the <see cref="ObjectField"/>.
        /// </summary>
        /// <param name="value">The object field name.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="value"/> is <c>null</c> or
        /// <see cref="string.Empty"/>.
        /// </exception>
        IObjectFieldDescriptor Name(NameString value);

        /// <summary>
        /// Adds explanatory text to the <see cref="ObjectField"/>
        /// that can be accessed via introspection.
        /// </summary>
        /// <param name="value">The object field description.</param>
        IObjectFieldDescriptor Description(string? value);

        /// <summary>
        /// Specifies a deprecation reason for this field.
        /// </summary>
        /// <param name="reason">The reason why this field is deprecated.</param>
        [Obsolete("Use `Deprecated`.")]
        IObjectFieldDescriptor DeprecationReason(string? reason);

        /// <summary>
        /// Deprecates the object field.
        /// </summary>
        /// <param name="reason">The reason why this field is deprecated.</param>
        IObjectFieldDescriptor Deprecated(string? reason);

        /// <summary>
        /// Deprecates the object field.
        /// </summary>
        IObjectFieldDescriptor Deprecated();

        /// <summary>
        /// Defines the type of the object field.
        /// </summary>
        /// <typeparam name="TOutputType">
        /// The type.
        /// </typeparam>
        IObjectFieldDescriptor Type<TOutputType>()
            where TOutputType : class, IOutputType;

        /// <summary>
        /// Defines the type of the object field.
        /// </summary>
        /// <typeparam name="TOutputType">
        /// The type.
        /// </typeparam>
        /// <param name="outputType">
        /// The output type instance.
        /// </param>
        IObjectFieldDescriptor Type<TOutputType>(TOutputType outputType)
            where TOutputType : class, IOutputType;

        /// <summary>
        /// Defines the type of the object field.
        /// </summary>
        IObjectFieldDescriptor Type(ITypeNode typeNode);

        /// <summary>
        /// Defines the type of the object field.
        /// </summary>
        IObjectFieldDescriptor Type(Type type);

        /// <summary>
        /// Defines a field argument.
        /// </summary>
        /// <param name="argumentName">
        /// The field argument name.
        /// </param>
        /// <param name="argumentDescriptor">
        /// The argument descriptor to specify the argument configuration. 
        /// </param>
        IObjectFieldDescriptor Argument(
            NameString argumentName,
            Action<IArgumentDescriptor> argumentDescriptor);

        IObjectFieldDescriptor Ignore(bool ignore = true);

        [Obsolete("Use Resolve(...)")]
        IObjectFieldDescriptor Resolver(
            FieldResolverDelegate fieldResolver);

        [Obsolete("Use Resolve(...)")]
        IObjectFieldDescriptor Resolver(
            FieldResolverDelegate fieldResolver,
            Type resultType);

        IObjectFieldDescriptor Resolve(
            FieldResolverDelegate fieldResolver);

        IObjectFieldDescriptor Resolve(
            FieldResolverDelegate fieldResolver,
            Type resultType);

        IObjectFieldDescriptor ResolveWith<TResolver>(
            Expression<Func<TResolver, object?>> propertyOrMethod);

        IObjectFieldDescriptor ResolveWith(MemberInfo propertyOrMethod);

        IObjectFieldDescriptor Subscribe(
            SubscribeResolverDelegate subscribeResolver);

        IObjectFieldDescriptor Use(
            FieldMiddleware middleware);

        IObjectFieldDescriptor Directive<T>(
            T directiveInstance)
            where T : class;

        IObjectFieldDescriptor Directive<T>()
            where T : class, new();

        IObjectFieldDescriptor Directive(
            NameString name,
            params ArgumentNode[] arguments);

        IObjectFieldDescriptor ConfigureContextData(Action<ExtensionData> configure);
    }
}
