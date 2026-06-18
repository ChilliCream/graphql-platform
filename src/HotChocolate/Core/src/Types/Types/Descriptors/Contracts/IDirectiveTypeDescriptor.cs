using System.Diagnostics.CodeAnalysis;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors.Configurations;

namespace HotChocolate.Types;

public interface IDirectiveTypeDescriptor
    : IDescriptor<DirectiveTypeConfiguration>
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
    IDirectiveTypeDescriptor Name(string value);

    /// <summary>
    /// Adds explanatory text to the <see cref="DirectiveType"/>
    /// that can be accessed via introspection.
    /// </summary>
    /// <param name="value">The object type description.</param>
    IDirectiveTypeDescriptor Description(string value);

    /// <summary>
    /// Deprecates the directive.
    /// </summary>
    /// <param name="reason">The reason why this directive is deprecated.</param>
    IDirectiveTypeDescriptor Deprecated(string? reason);

    /// <summary>
    /// Deprecates the directive.
    /// </summary>
    IDirectiveTypeDescriptor Deprecated();

    /// <summary>
    /// Specifies a directive argument.
    /// </summary>
    /// <param name="name">The name of the argument.</param>
    IDirectiveArgumentDescriptor Argument(string name);

    /// <summary>
    /// Specifies in which location the directive belongs in.
    /// </summary>
    /// <param name="value">The directive location.</param>
    IDirectiveTypeDescriptor Location(DirectiveLocation value);

    /// <summary>
    /// Configure a middleware for this directive.
    /// </summary>
    IDirectiveTypeDescriptor Use(
        DirectiveMiddleware middleware);

    /// <summary>
    /// Configure a middleware for this directive.
    /// </summary>
    IDirectiveTypeDescriptor Use<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods)] TMiddleware>()
        where TMiddleware : class;

    /// <summary>
    /// Configure a middleware for this directive.
    /// </summary>
    /// <param name="factory">The middleware factory.</param>
    IDirectiveTypeDescriptor Use<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods)] TMiddleware>(
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

    /// <summary>
    /// Annotates a directive to this directive definition.
    /// </summary>
    IDirectiveTypeDescriptor Directive<T>(T directiveInstance)
        where T : class;

    /// <summary>
    /// Annotates a directive to this directive definition.
    /// </summary>
    IDirectiveTypeDescriptor Directive<T>()
        where T : class, new();

    /// <summary>
    /// Annotates a directive to this directive definition.
    /// </summary>
    IDirectiveTypeDescriptor Directive(string name, params ArgumentNode[] arguments);
}
