using System;
using System.Linq.Expressions;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    public interface IInterfaceTypeDescriptor
        : IFluent
    {
        // <summary>
        /// Associates the specified <paramref name="syntaxNode"/>
        /// with the <see cref="InterfaceType"/>.
        /// </summary>
        /// <param name="syntaxNode">
        /// The <see cref="InterfaceTypeDefinitionNode"/> of a parsed schema.
        /// </param>
        IInterfaceTypeDescriptor SyntaxNode(
            InterfaceTypeDefinitionNode syntaxNode);

        /// <summary>
        /// Defines the name of the <see cref="InterfaceType"/>.
        /// </summary>
        /// <param name="name">The object type name.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="name"/> is <c>null</c> or
        /// <see cref="string.Empty"/>.
        /// </exception>
        IInterfaceTypeDescriptor Name(NameString name);

        /// <summary>
        /// Adds explanatory text to the <see cref="ObjectType"/>
        /// that can be accessd via introspection.
        /// </summary>
        /// <param name="description">The object type description.</param>
        IInterfaceTypeDescriptor Description(string description);

        IInterfaceTypeDescriptor ResolveAbstractType(
            ResolveAbstractType resolveAbstractType);

        IInterfaceFieldDescriptor Field(NameString name);

        IInterfaceTypeDescriptor Directive<T>(T directive)
            where T : class;

        IInterfaceTypeDescriptor Directive<T>()
            where T : class, new();

        IInterfaceTypeDescriptor Directive(
            NameString name,
            params ArgumentNode[] arguments);

        IInterfaceTypeDescriptor Directive(
            string name,
            params ArgumentNode[] arguments);
    }

    public interface IInterfaceTypeDescriptor<T>
        : IInterfaceTypeDescriptor
    {
        new IInterfaceTypeDescriptor<T> SyntaxNode(
            InterfaceTypeDefinitionNode syntaxNode);

        new IInterfaceTypeDescriptor<T> Name(NameString name);

        new IInterfaceTypeDescriptor<T> Description(string description);

        new IInterfaceTypeDescriptor<T> ResolveAbstractType(
            ResolveAbstractType resolveAbstractType);

        IInterfaceFieldDescriptor Field(
            Expression<Func<T, object>> propertyOrMethod);

        new IInterfaceTypeDescriptor<T> Directive<T>(T directive)
            where T : class;

        new IInterfaceTypeDescriptor<T> Directive<T>()
            where T : class, new();

        new IInterfaceTypeDescriptor<T> Directive(
            NameString name,
            params ArgumentNode[] arguments);

        new IInterfaceTypeDescriptor<T> Directive(
            string name,
            params ArgumentNode[] arguments);
    }
}
