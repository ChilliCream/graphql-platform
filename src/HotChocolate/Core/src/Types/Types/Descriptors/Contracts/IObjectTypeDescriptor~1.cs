using System;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types
{
    public interface IObjectTypeDescriptor<T>
        : IDescriptor<ObjectTypeDefinition>
        , IFluent
    {
        /// <summary>
        /// Defines the name of the <see cref="ObjectType"/>.
        /// </summary>
        /// <param name="value">The object type name.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="value"/> is <c>null</c> or
        /// <see cref="string.Empty"/>.
        /// </exception>
        IObjectTypeDescriptor<T> Name(NameString value);

        /// <summary>
        /// Adds explanatory text of the <see cref="ObjectType"/>
        /// that can be accessed via introspection.
        /// </summary>
        /// <param name="value">The object type description.</param>
        IObjectTypeDescriptor<T> Description(string value);

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
        IObjectTypeDescriptor<T> BindFields(BindingBehavior behavior);

           /// <summary>
        /// Defines that all fields have to be specified explicitly.
        /// </summary>
        IObjectTypeDescriptor<T> BindFieldsExplicitly();

        /// <summary>
        /// Defines that all fields shall be inferred
        /// from the associated .Net type,
        /// </summary>
        IObjectTypeDescriptor<T> BindFieldsImplicitly();

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
        IObjectTypeDescriptor<T> Interface<TInterface>(TInterface type)
            where TInterface : InterfaceType;

        /// <summary>
        /// Specifies an interface that is implemented by the
        /// <see cref="ObjectType"/>.
        /// </summary>
        /// <param name="type">
        /// A syntax node representing an interface type.
        /// </param>
        IObjectTypeDescriptor<T> Interface(NamedTypeNode type);

        /// <summary>
        /// Specifies an interface that is implemented by the
        /// <see cref="ObjectType"/>.
        /// </summary>
        /// <typeparam name="T">The interface type.</typeparam>
        IObjectTypeDescriptor<T> Implements<TInterface>()
            where TInterface : InterfaceType;

        /// <summary>
        /// Specifies an interface that is implemented by the
        /// <see cref="ObjectType"/>.
        /// </summary>
        /// <typeparam name="T">The interface type.</typeparam>
        IObjectTypeDescriptor<T> Implements<TInterface>(TInterface type)
            where TInterface : InterfaceType;

        /// <summary>
        /// Specifies an interface that is implemented by the
        /// <see cref="ObjectType"/>.
        /// </summary>
        /// <param name="type">
        /// A syntax node representing an interface type.
        /// </param>
        IObjectTypeDescriptor<T> Implements(NamedTypeNode type);

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

        /// <summary>
        /// Specifies an object type field.
        /// </summary>
        /// <param name="propertyOrMethod">
        /// An expression selecting a property or method of
        /// <typeparamref name="T"/>.
        /// </param>
        IObjectFieldDescriptor Field<TValue>(
            Expression<Func<T, TValue>> propertyOrMethod);

        /// <summary>
        /// Specifies an object type field.
        /// </summary>
        /// <param name="name">
        /// The name that the field shall have.
        /// </param>
        IObjectFieldDescriptor Field(NameString name);

        /// <summary>
        /// Specifies an object type field which is bound to a resolver type.
        /// </summary>
        /// <param name="propertyOrMethod">
        /// An expression selecting a property or method of
        /// <typeparamref name="TResolver"/>.
        /// The resolver type containing the property or method.
        /// </param>
        IObjectFieldDescriptor Field<TResolver>(
            Expression<Func<TResolver, object>> propertyOrMethod);

        /// <summary>
        /// Specifies an object type field which is bound to a resolver type.
        /// </summary>
        /// <param name="propertyOrMethod">
        /// The member representing a field.
        /// </param>
        IObjectFieldDescriptor Field(MemberInfo propertyOrMethod);

        IObjectTypeDescriptor<T> Directive<TDirective>(
            TDirective directiveInstance)
            where TDirective : class;

        IObjectTypeDescriptor<T> Directive<TDirective>()
            where TDirective : class, new();

        IObjectTypeDescriptor<T> Directive(
            NameString name,
            params ArgumentNode[] arguments);
    }
}
