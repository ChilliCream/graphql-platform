using System;
using System.Linq.Expressions;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types
{
    public interface IInterfaceTypeDescriptor<T>
        : IDescriptor<InterfaceTypeDefinition>
        , IFluent
    {
        // <summary>
        /// Associates the specified
        /// <paramref name="syntaxinterfaceTypeDefinitionode"/>
        /// with the <see cref="InterfaceType"/>.
        /// </summary>
        /// <param name="interfaceTypeDefinition">
        /// The <see cref="InterfaceTypeDefinitionNode"/> of a parsed schema.
        /// </param>
        IInterfaceTypeDescriptor<T> SyntaxNode(
            InterfaceTypeDefinitionNode syntaxinterfaceTypeDefinitionode);

        /// <summary>
        /// Defines the name of the <see cref="InterfaceType"/>.
        /// </summary>
        /// <param name="value">The interface type name.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="value"/> is <c>null</c> or
        /// <see cref="string.Empty"/>.
        /// </exception>
        IInterfaceTypeDescriptor<T> Name(NameString value);

        /// <summary>
        /// Adds explanatory text to the <see cref="InterfaceType"/>
        /// that can be accessd via introspection.
        /// </summary>
        /// <param name="value">The interface type description.</param>
        IInterfaceTypeDescriptor<T> Description(string value);

        /// <summary>
        /// Defines the field binding behavior.
        ///
        /// The default binding behavior is set to
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

        /// <summary>
        /// Defines that all fields have to be specified explicitly.
        /// </summary>
        IInterfaceTypeDescriptor<T> BindFieldsExplicitly();

        /// <summary>
        /// Defines that all fields shall be infered
        /// from the associated .Net type,
        /// </summary>
        IInterfaceTypeDescriptor<T> BindFieldsImplicitly();

        IInterfaceTypeDescriptor<T> ResolveAbstractType(
            ResolveAbstractType typeResolver);

        IInterfaceFieldDescriptor Field(
            Expression<Func<T, object>> propertyOrMethod);

        IInterfaceFieldDescriptor Field(NameString name);

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
