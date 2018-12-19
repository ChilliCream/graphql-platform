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
        /// <summary>
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
        IDirectiveTypeDescriptor Name(NameString name);

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
        IArgumentDescriptor Argument(NameString name);

        /// <summary>
        /// Specifies in which location the directive belongs in.
        /// </summary>
        /// <param name="location">The directive location.</param>
        IDirectiveTypeDescriptor Location(DirectiveLocation location);

        // TODO : DOCU
        IDirectiveTypeDescriptor Middleware(
            DirectiveMiddleware middleware);

        // TODO : DOCU
        IDirectiveTypeDescriptor Middleware<T>(
            Expression<Func<T, object>> method);

        // TODO : DOCU
        IDirectiveTypeDescriptor Middleware<T>(
            Expression<Action<T>> method);
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
        new IDirectiveTypeDescriptor<T> Name(NameString name);

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

        // TODO : DOCU
        new IDirectiveTypeDescriptor Middleware(
            DirectiveMiddleware middleware);

        // TODO : DOCU
        new IDirectiveTypeDescriptor Middleware<TMiddleware>(
            Expression<Func<TMiddleware, object>> method);

        // TODO : DOCU
        IDirectiveTypeDescriptor Middleware<TMiddleware>(
            Expression<Action<T>> method);
    }
}
