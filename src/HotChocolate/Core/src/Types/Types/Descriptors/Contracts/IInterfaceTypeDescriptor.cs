using System;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types
{
    public interface IInterfaceTypeDescriptor
        : IDescriptor<InterfaceTypeDefinition>
        , IFluent
    {
        // <summary>
        /// Associates the specified
        /// <paramref name="interfaceTypeDefinition"/>
        /// with the <see cref="InterfaceType"/>.
        /// </summary>
        /// <param name="syntaxNode">
        /// The <see cref="InterfaceTypeDefinitionNode"/> of a parsed schema.
        /// </param>
        IInterfaceTypeDescriptor SyntaxNode(
            InterfaceTypeDefinitionNode interfaceTypeDefinition);

        /// <summary>
        /// Defines the name of the <see cref="InterfaceType"/>.
        /// </summary>
        /// <param name="value">The interface type name.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="value"/> is <c>null</c> or
        /// <see cref="string.Empty"/>.
        /// </exception>
        IInterfaceTypeDescriptor Name(NameString value);

        /// <summary>
        /// Adds explanatory text to the <see cref="InterfaceType"/>
        /// that can be accessd via introspection.
        /// </summary>
        /// <param name="value">The interface type description.</param>
        IInterfaceTypeDescriptor Description(string value);

        IInterfaceTypeDescriptor ResolveAbstractType(
            ResolveAbstractType typeResolver);

        IInterfaceFieldDescriptor Field(NameString name);

        IInterfaceTypeDescriptor Directive<T>(T directiveInstance)
            where T : class;

        IInterfaceTypeDescriptor Directive<T>()
            where T : class, new();

        IInterfaceTypeDescriptor Directive(
            NameString name,
            params ArgumentNode[] arguments);
    }
}
