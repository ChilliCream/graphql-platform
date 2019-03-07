using System;
using System.Linq.Expressions;
using HotChocolate.Language;
using HotChocolate.Resolvers;

namespace HotChocolate.Types
{
    public interface IDirectiveTypeDescriptor<T>
        : IFluent
    {
        // <summary>
        /// Associates the specified <paramref name="directiveDefinitionNode"/>
        /// with the <see cref="DirectiveType"/>.
        /// </summary>
        /// <param name="syntaxNode">
        /// The <see cref="DirectiveDefinitionNode"/> of a parsed schema.
        /// </param>

        IDirectiveTypeDescriptor<T> SyntaxNode(
            DirectiveDefinitionNode directiveDefinitionNode);

        /// <summary>
        /// Defines the name of the <see cref="DirectiveType"/>.
        /// </summary>
        /// <param name="value">The directive type name.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="value"/> is <c>null</c> or <see cref="string.Empty"/>.
        /// </exception>
        IDirectiveTypeDescriptor<T> Name(NameString value);

        /// <summary>
        /// Adds explanatory text to the <see cref="DirectiveType"/>
        /// that can be accessd via introspection.
        /// </summary>
        /// <param name="value">The object type description.</param>
        IDirectiveTypeDescriptor<T> Description(string value);

        /// <summary>
        /// Defines the argument binding behavior.
        ///
        /// The default binding behaviour is set to
        /// <see cref="BindingBehavior.Implicit"/>.
        /// </summary>
        /// <param name="behavior">
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
            BindingBehavior behavior);

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
        /// <param name="value">The directive location.</param>
        IDirectiveTypeDescriptor<T> Location(DirectiveLocation value);

        // TODO : DOCU
        [Obsolete]
        IDirectiveTypeDescriptor<T> Middleware(
            DirectiveMiddleware middleware);

        // TODO : DOCU
        [Obsolete]
        IDirectiveTypeDescriptor<T> Middleware<TMiddleware>(
            Expression<Func<TMiddleware, object>> method);

        // TODO : DOCU
        [Obsolete]
        IDirectiveTypeDescriptor<T> Middleware<TMiddleware>(
            Expression<Action<T>> method);

        IDirectiveTypeDescriptor<T> Use(
            DirectiveMiddleware middleware);

        /// <summary>
        /// Allows this directive type to be declared multiple times in a single location.
        /// </summary>
        IDirectiveTypeDescriptor<T> Repeatable();
    }
}
