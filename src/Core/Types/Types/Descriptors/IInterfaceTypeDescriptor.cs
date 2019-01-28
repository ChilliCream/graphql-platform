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
        /// <param name="name">The interface type name.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="name"/> is <c>null</c> or
        /// <see cref="string.Empty"/>.
        /// </exception>
        IInterfaceTypeDescriptor Name(NameString name);

        /// <summary>
        /// Adds explanatory text to the <see cref="InterfaceType"/>
        /// that can be accessd via introspection.
        /// </summary>
        /// <param name="description">The interface type description.</param>
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
        // <summary>
        /// Associates the specified <paramref name="syntaxNode"/>
        /// with the <see cref="InterfaceType"/>.
        /// </summary>
        /// <param name="syntaxNode">
        /// The <see cref="InterfaceTypeDefinitionNode"/> of a parsed schema.
        /// </param>
        new IInterfaceTypeDescriptor<T> SyntaxNode(
            InterfaceTypeDefinitionNode syntaxNode);

        /// <summary>
        /// Defines the name of the <see cref="InterfaceType"/>.
        /// </summary>
        /// <param name="name">The interface type name.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="name"/> is <c>null</c> or
        /// <see cref="string.Empty"/>.
        /// </exception>
        new IInterfaceTypeDescriptor<T> Name(NameString name);

        /// <summary>
        /// Adds explanatory text to the <see cref="InterfaceType"/>
        /// that can be accessd via introspection.
        /// </summary>
        /// <param name="description">The interface type description.</param>
        new IInterfaceTypeDescriptor<T> Description(string description);

        /// <summary>
        /// Defines the field binding behavior.
        ///
        /// The default binding behaviour is set to
        /// <see cref="BindingBehavior.Implicit"/>.
        /// </summary>
        /// <param name="bindingBehavior">
        /// The binding behavior.
        ///
        /// Implicit:
        /// The object type descriptor will try to infer the object type
        /// fields from the specified .net object type representation
        /// (<typeparamref name="T"/>).
        ///
        /// Explicit:
        /// All field have to be specified explicitly via
        /// <see cref="Field(Expression{Func{T, object}})"/>
        /// or <see cref="Field(string)"/>.
        /// </param>
        IInterfaceTypeDescriptor<T> BindFields(BindingBehavior bindingBehavior);

        new IInterfaceTypeDescriptor<T> ResolveAbstractType(
            ResolveAbstractType resolveAbstractType);

        IInterfaceFieldDescriptor Field(
            Expression<Func<T, object>> propertyOrMethod);

        new IInterfaceTypeDescriptor<T> Directive<TDirective>(
            TDirective directive)
            where TDirective : class;

        new IInterfaceTypeDescriptor<T> Directive<TDirective>()
            where TDirective : class, new();

        new IInterfaceTypeDescriptor<T> Directive(
            NameString name,
            params ArgumentNode[] arguments);

        new IInterfaceTypeDescriptor<T> Directive(
            string name,
            params ArgumentNode[] arguments);
    }
}
