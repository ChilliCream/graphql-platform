using System;
using System.Linq.Expressions;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    public interface IInterfaceTypeDescriptor<T>
        : IFluent
    {
        // <summary>
        /// Associates the specified <paramref name="syntaxNode"/>
        /// with the <see cref="InterfaceType"/>.
        /// </summary>
        /// <param name="syntaxNode">
        /// The <see cref="InterfaceTypeDefinitionNode"/> of a parsed schema.
        /// </param>
        IInterfaceTypeDescriptor<T> SyntaxNode(
            InterfaceTypeDefinitionNode syntaxNode);

        /// <summary>
        /// Defines the name of the <see cref="InterfaceType"/>.
        /// </summary>
        /// <param name="name">The interface type name.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="name"/> is <c>null</c> or
        /// <see cref="string.Empty"/>.
        /// </exception>
        IInterfaceTypeDescriptor<T> Name(NameString name);

        /// <summary>
        /// Adds explanatory text to the <see cref="InterfaceType"/>
        /// that can be accessd via introspection.
        /// </summary>
        /// <param name="description">The interface type description.</param>
        IInterfaceTypeDescriptor<T> Description(string description);

        /// <summary>
        /// Defines the field binding behavior.
        ///
        /// The default binding behaviour is set to
        /// <see cref="BindingBehavior.Implicit"/>.
        /// </summary>
        /// <param name="behavior">
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
        IInterfaceTypeDescriptor<T> BindFields(BindingBehavior behavior);

        IInterfaceTypeDescriptor<T> ResolveAbstractType(
            ResolveAbstractType typeResolver);

        IInterfaceFieldDescriptor Field(
            Expression<Func<T, object>> propertyOrMethod);

        IInterfaceTypeDescriptor<T> Directive<TDirective>(
            TDirective directiveInstance)
            where TDirective : class;

        IInterfaceTypeDescriptor<T> Directive<TDirective>()
            where TDirective : class, new();

        IInterfaceTypeDescriptor<T> Directive(
            NameString name,
            params ArgumentNode[] arguments);
    }
}
