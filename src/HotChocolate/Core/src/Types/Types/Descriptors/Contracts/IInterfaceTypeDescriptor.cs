using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types;

public interface IInterfaceTypeDescriptor
    : IDescriptor<InterfaceTypeDefinition>
    , IFluent
{
    /// <summary>
    /// Defines the name of the <see cref="InterfaceType"/>.
    /// </summary>
    /// <param name="value">The interface type name.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="value"/> is <c>null</c> or
    /// <see cref="string.Empty"/>.
    /// </exception>
    IInterfaceTypeDescriptor Name(string value);

    /// <summary>
    /// Adds explanatory text to the <see cref="InterfaceType"/>
    /// that can be accessed via introspection.
    /// </summary>
    /// <param name="value">The interface type description.</param>
    IInterfaceTypeDescriptor Description(string value);

    /// <summary>
    /// Specifies an interface that is implemented by the
    /// <see cref="InterfaceType"/>.
    /// </summary>
    /// <typeparam name="T">The interface type.</typeparam>
    IInterfaceTypeDescriptor Implements<T>()
        where T : InterfaceType;

    /// <summary>
    /// Specifies an interface that is implemented by the
    /// <see cref="InterfaceType"/>.
    /// </summary>
    /// <typeparam name="T">The interface type.</typeparam>
    IInterfaceTypeDescriptor Implements<T>(T type)
        where T : InterfaceType;

    /// <summary>
    /// Specifies an interface that is implemented by the
    /// <see cref="InterfaceType"/>.
    /// </summary>
    /// <param name="type">
    /// A syntax node representing an interface type.
    /// </param>
    IInterfaceTypeDescriptor Implements(NamedTypeNode type);

    IInterfaceTypeDescriptor ResolveAbstractType(
        ResolveAbstractType typeResolver);

    IInterfaceFieldDescriptor Field(string name);

    IInterfaceTypeDescriptor Directive<T>(T directiveInstance)
        where T : class;

    IInterfaceTypeDescriptor Directive<T>()
        where T : class, new();

    IInterfaceTypeDescriptor Directive(
        string name,
        params ArgumentNode[] arguments);
}
