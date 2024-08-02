using System.Linq.Expressions;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types;

public interface IDirectiveTypeDescriptor<T>
    : IDescriptor<DirectiveTypeDefinition>
    , IFluent
{
    /// <summary>
    /// Defines the name of the <see cref="DirectiveType"/>.
    /// </summary>
    /// <param name="value">The directive type name.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="value"/> is <c>null</c> or
    /// <see cref="string.Empty"/>.
    /// </exception>
    IDirectiveTypeDescriptor<T> Name(string value);

    /// <summary>
    /// Adds explanatory text to the <see cref="DirectiveType"/>
    /// that can be accessed via introspection.
    /// </summary>
    /// <param name="value">The object type description.</param>
    IDirectiveTypeDescriptor<T> Description(string value);

    /// <summary>
    /// Defines the argument binding behavior.
    ///
    /// The default binding behavior is set to
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
    /// <see cref="Argument(Expression{Func{T,object}})"/>.
    /// </param>
    IDirectiveTypeDescriptor<T> BindArguments(
        BindingBehavior behavior);

    /// <summary>
    /// Defines that all arguments have to be specified explicitly.
    /// </summary>
    IDirectiveTypeDescriptor<T> BindArgumentsExplicitly();

    /// <summary>
    /// The directive type will add arguments for all compatible properties.
    /// </summary>
    IDirectiveTypeDescriptor<T> BindArgumentsImplicitly();

    /// <summary>
    /// Specifies a directive argument.
    /// </summary>
    /// <param name="property">
    /// An expression selecting a property <typeparamref name="T"/>.
    /// </param>
    IDirectiveArgumentDescriptor Argument(
        Expression<Func<T, object>> property);

    /// <summary>
    /// Specifies a directive argument.
    /// </summary>
    /// <param name="name">The name of the argument.</param>
    IDirectiveArgumentDescriptor Argument(string name);

    /// <summary>
    /// Specifies in which location the directive belongs in.
    /// </summary>
    /// <param name="value">The directive location.</param>
    IDirectiveTypeDescriptor<T> Location(DirectiveLocation value);

    /// <summary>
    /// Configure a middleware for this directive.
    /// </summary>
    IDirectiveTypeDescriptor<T> Use(
        DirectiveMiddleware middleware);

    /// <summary>
    /// Configure a middleware for this directive.
    /// </summary>
    IDirectiveTypeDescriptor<T> Use<TMiddleware>()
        where TMiddleware : class;

    /// <summary>
    /// Configure a middleware for this directive.
    /// </summary>
    /// <param name="factory">The middleware factory.</param>
    IDirectiveTypeDescriptor<T> Use<TMiddleware>(
        Func<IServiceProvider, FieldDelegate, TMiddleware> factory)
        where TMiddleware : class;

    /// <summary>
    /// Allows this directive type to be declared multiple
    /// times in a single location.
    /// </summary>
    IDirectiveTypeDescriptor<T> Repeatable();

    /// <summary>
    /// Directive is public and visible within the type system and through introspection.
    /// </summary>
    IDirectiveTypeDescriptor<T> Public();

    /// <summary>
    /// Directive is internal and only visible within the type system.
    /// </summary>
    IDirectiveTypeDescriptor<T> Internal();
}
