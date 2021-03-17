using System;
using System.Linq.Expressions;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types
{
    public interface IDirectiveTypeDescriptor
        : IDescriptor<DirectiveTypeDefinition>
        , IFluent
    {
        /// <summary>
        /// Associates the specified <paramref name="directiveDefinitionNode"/>
        /// with the <see cref="DirectiveType"/>.
        /// </summary>
        /// <param name="directiveDefinitionNode">
        /// The <see cref="DirectiveDefinitionNode"/> of a parsed schema.
        /// </param>
        IDirectiveTypeDescriptor SyntaxNode(
            DirectiveDefinitionNode directiveDefinitionNode);

        /// <summary>
        /// Defines the name of the <see cref="DirectiveType"/>.
        /// </summary>
        /// <param name="value">The directive type name.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="value"/> is <c>null</c> or
        /// <see cref="string.Empty"/>.
        /// </exception>
        IDirectiveTypeDescriptor Name(NameString value);

        /// <summary>
        /// Adds explanatory text to the <see cref="DirectiveType"/>
        /// that can be accessed via introspection.
        /// </summary>
        /// <param name="value">The object type description.</param>
        IDirectiveTypeDescriptor Description(string value);

        /// <summary>
        /// Specifies a directive argument.
        /// </summary>
        /// <param name="name">The name of the argument.</param>
        IDirectiveArgumentDescriptor Argument(NameString name);

        /// <summary>
        /// Specifies in which location the directive belongs in.
        /// </summary>
        /// <param name="value">The directive location.</param>
        IDirectiveTypeDescriptor Location(DirectiveLocation value);

        [Obsolete("Replace Middleware with `Use`.")]
        IDirectiveTypeDescriptor Middleware(
            DirectiveMiddleware middleware);

        [Obsolete("Replace Middleware with `Use`.", true)]
        IDirectiveTypeDescriptor Middleware<T>(
            Expression<Func<T, object>> method);

        [Obsolete("Replace Middleware with `Use`.", true)]
        IDirectiveTypeDescriptor Middleware<T>(
            Expression<Action<T>> method);

        /// <summary>
        /// Configure a middleware for this directive.
        /// </summary>
        IDirectiveTypeDescriptor Use(
            DirectiveMiddleware middleware);

        /// <summary>
        /// Configure a middleware for this directive.
        /// </summary>
        IDirectiveTypeDescriptor Use<TMiddleware>()
            where TMiddleware : class;

        /// <summary>
        /// Configure a middleware for this directive.
        /// </summary>
        /// <param name="factory">The middleware factory.</param>
        IDirectiveTypeDescriptor Use<TMiddleware>(
            Func<IServiceProvider, FieldDelegate, TMiddleware> factory)
            where TMiddleware : class;

        /// <summary>
        /// Allows this directive type to be declared multiple times
        /// in a single location.
        /// </summary>
        IDirectiveTypeDescriptor Repeatable();

        /// <summary>
        /// Directive is public and visible within the type system and through introspection.
        /// </summary>
        IDirectiveTypeDescriptor Public();

        /// <summary>
        /// Directive is internal and only visible within the type system.
        /// </summary>
        IDirectiveTypeDescriptor Internal();
    }
}
