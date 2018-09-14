using System;
using System.Linq.Expressions;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Resolvers;

namespace HotChocolate.Types
{
    public interface IDirectiveTypeDescriptor
        : IFluent
    {
        // <summary>
        /// Associates the specified <paramref name="syntaxNode"/>
        /// with the <see cref="DirectiveType"/>.
        /// </summary>
        /// <param name="syntaxNode">
        /// The <see cref="DirectiveDefinitionNode"/> of a parsed schema.
        /// </param>

        IDirectiveTypeDescriptor SyntaxNode(DirectiveDefinitionNode syntaxNode);

        /// <summary>
        /// Defines the name of the <see cref="DirectiveType"/>.
        /// </summary>
        /// <param name="name">The directive type name.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="name"/> is <c>null</c> or
        /// <see cref="string.Empty"/>.
        /// </exception>
        IDirectiveTypeDescriptor Name(string name);

        /// <summary>
        /// Adds explanatory text to the <see cref="DirectiveType"/>
        /// that can be accessd via introspection.
        /// </summary>
        /// <param name="description">The object type description.</param>
        IDirectiveTypeDescriptor Description(string description);

        /// <summary>
        /// Specifies a directive argument.
        /// </summary>
        /// <param name="name">The name of the argument.</param>
        IArgumentDescriptor Argument(string name);

        /// <summary>
        /// Specifies in which location the directive belongs in.
        /// </summary>
        /// <param name="location">The directive location.</param>
        IDirectiveTypeDescriptor Location(DirectiveLocation location);

        /// <summary>
        /// Specifies a resolver for this directive that will be chained
        /// into the field resolver pipeline.
        /// </summary>
        /// <param name="resolver">
        /// The delegate that represents the resolver.
        /// </param>
        IDirectiveTypeDescriptor Resolver(DirectiveResolver resolver);

        /// <summary>
        /// Specifies a resolver for this directive that will be chained
        /// into the field resolver pipeline.
        /// </summary>
        /// <param name="resolver">
        /// The delegate that represents the resolver.
        /// </param>
        IDirectiveTypeDescriptor Resolver(AsyncDirectiveResolver resolver);

        /// <summary>
        /// Specifies a resolver for this directive that will be chained
        /// into the field resolver pipeline.
        /// </summary>
        /// <param name="method">
        /// The method that shall be used as a resolver,
        /// </param>
        /// <typeparam name="TResolver">
        /// The type that contains the resolver method.
        /// </typeparam>
        IDirectiveTypeDescriptor Resolver<TResolver>(
            Expression<Func<TResolver, object>> method);
    }

    public interface IDirectiveTypeDescriptor<T>
        : IDirectiveTypeDescriptor
    {
        // <summary>
        /// Associates the specified <paramref name="syntaxNode"/>
        /// with the <see cref="DirectiveType"/>.
        /// </summary>
        /// <param name="syntaxNode">
        /// The <see cref="DirectiveDefinitionNode"/> of a parsed schema.
        /// </param>

        new IDirectiveTypeDescriptor<T> SyntaxNode(
            DirectiveDefinitionNode syntaxNode);

        /// <summary>
        /// Defines the name of the <see cref="DirectiveType"/>.
        /// </summary>
        /// <param name="name">The directive type name.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="name"/> is <c>null</c> or <see cref="string.Empty"/>.
        /// </exception>
        new IDirectiveTypeDescriptor<T> Name(string name);

        /// <summary>
        /// Adds explanatory text to the <see cref="DirectiveType"/>
        /// that can be accessd via introspection.
        /// </summary>
        /// <param name="description">The object type description.</param>
        new IDirectiveTypeDescriptor<T> Description(string description);

        /// <summary>
        /// Defines the argument binding behavior.
        ///
        /// The default binding behaviour is set to
        /// <see cref="BindingBehavior.Implicit"/>.
        /// </summary>
        /// <param name="bindingBehavior">
        /// The binding behavior.
        ///
        /// Implicit:
        /// The directive type descriptor will try to infer the directive type
        /// arguments from the specified .net directive type representation
        /// (<typeparamref name="T"/>).
        ///
        /// Explicit:
        /// All arguments have to specified explicitly via
        /// <see cref="IDirectiveTypeDescriptor{T}.Field{TValue}(Expression{Func{T, TValue}})"/>.
        /// </param>
        IDirectiveTypeDescriptor<T> BindArguments(
            BindingBehavior bindingBehavior);

        /// <summary>
        /// Specifies a directive argument.
        /// </summary>
        /// <param name="property">
        /// An expression selecting a property <typeparamref name="T"/>.
        /// </param>
        IDirectiveArgumentDescriptor Argument(
            Expression<Func<T, object>> property);

        /// <summary>
        /// Specifies in which location the directive belongs in.
        /// </summary>
        /// <param name="location">The directive location.</param>
        new IDirectiveTypeDescriptor<T> Location(DirectiveLocation location);

        /// <summary>
        /// Specifies a resolver for this directive that will be chained
        /// into the field resolver pipeline.
        /// </summary>
        /// <param name="resolver">
        /// The delegate that represents the resolver.
        /// </param>
        new IDirectiveTypeDescriptor<T> Resolver(DirectiveResolver resolver);

        /// <summary>
        /// Specifies a resolver for this directive that will be chained
        /// into the field resolver pipeline.
        /// </summary>
        /// <param name="resolver">
        /// The delegate that represents the resolver.
        /// </param>
        new IDirectiveTypeDescriptor<T> Resolver(AsyncDirectiveResolver resolver);

        /// <summary>
        /// Specifies a resolver for this directive that will be chained
        /// into the field resolver pipeline.
        /// </summary>
        /// <param name="method">
        /// The method that shall be used as a resolver,
        /// </param>
        /// <typeparam name="TResolver">
        /// The type that contains the resolver method.
        /// </typeparam>
        new IDirectiveTypeDescriptor<T> Resolver<TResolver>(
          Expression<Func<TResolver, object>> method);
    }
}
