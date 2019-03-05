using System;
using System.Linq.Expressions;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    public interface IObjectTypeDescriptor<T>
        : IFluent
    {
        /// <summary>
        /// Defines the name of the <see cref="ObjectType"/>.
        /// </summary>
        /// <param name="name">The object type name.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="name"/> is <c>null</c> or <see cref="string.Empty"/>.
        /// </exception>
        IObjectTypeDescriptor<T> Name(NameString name);

        /// <summary>
        /// Adds explanatory text of the <see cref="ObjectType"/>
        /// that can be accessd via introspection.
        /// </summary>
        /// <param name="description">The object type description.</param>
        IObjectTypeDescriptor<T> Description(string description);

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
        IObjectTypeDescriptor<T> BindFields(BindingBehavior bindingBehavior);

        /// <summary>
        /// Specifies an interface that is implemented by the
        /// <see cref="ObjectType"/>.
        /// </summary>
        /// <typeparam name="T">The interface type.</typeparam>
        IObjectTypeDescriptor<T> Interface<TInterface>()
            where TInterface : InterfaceType;

        /// <summary>
        /// Specifies an interface that is implemented by the
        /// <see cref="ObjectType"/>.
        /// </summary>
        /// <typeparam name="T">The interface type.</typeparam>
        IObjectTypeDescriptor<T> Interface<TInterface>(
            TInterface type)
            where TInterface : InterfaceType;

        /// <summary>
        /// Includes a resolver type and imports all the methods and
        /// fields from it.
        /// </summary>
        /// <typeparam name="TResolver">A resolver type.</typeparam>
        IObjectTypeDescriptor<T> Include<TResolver>();

        /// <summary>
        /// Specifies a delegate that can determine if a resolver result
        /// represents an object instance of this <see cref="ObjectType"/>.
        /// </summary>
        /// <param name="isOfType">
        /// The delegate that provides the IsInstanceOfType functionality.
        /// </param>
        IObjectTypeDescriptor<T> IsOfType(IsOfType isOfType);

        /// <summary>
        /// Specifies an object type field.
        /// </summary>
        /// <param name="propertyOrMethod">
        /// An expression selecting a property or method of
        /// <typeparamref name="T"/>.
        /// </param>
        IObjectFieldDescriptor Field(
            Expression<Func<T, object>> propertyOrMethod);

        IObjectTypeDescriptor<T> Directive<TDirective>(
            TDirective directive)
            where TDirective : class;

        IObjectTypeDescriptor<T> Directive<TDirective>()
            where TDirective : class, new();

        IObjectTypeDescriptor<T> Directive(
            NameString name,
            params ArgumentNode[] arguments);
    }


}
