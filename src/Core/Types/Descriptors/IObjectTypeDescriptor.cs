using System;
using System.Linq.Expressions;
using HotChocolate.Configuration;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    public interface IObjectTypeDescriptor
        : IFluent
    {
        /// <summary>
        /// Associates the specified <paramref name="syntaxNode"/>
        /// with the <see cref="ObjectType"/>.
        /// </summary>
        /// <param name="syntaxNode">
        /// The <see cref="ObjectTypeDefinitionNode"/> of a parsed schema.
        /// </param>
        IObjectTypeDescriptor SyntaxNode(ObjectTypeDefinitionNode syntaxNode);

        /// <summary>
        /// Defines the name of the <see cref="ObjectType"/>.
        /// </summary>
        /// <param name="name">The object type name.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="name"/> is <c>null</c> or <see cref="string.Empty"/>.
        /// </exception>
        IObjectTypeDescriptor Name(string name);

        /// <summary>
        /// Adds explanatory text of the <see cref="ObjectType"/>
        /// that can be accessd via introspection.
        /// </summary>
        /// <param name="description">The object type description.</param>
        IObjectTypeDescriptor Description(string description);

        /// <summary>
        /// Specifies an interface that is implemented by the <see cref="ObjectType"/>.
        /// </summary>
        /// <typeparam name="T">The interface type.</typeparam>
        IObjectTypeDescriptor Interface<T>()
            where T : InterfaceType;


        /// <summary>
        /// Specifies an interface that is implemented by the <see cref="ObjectType"/>.
        /// </summary>
        /// <param name="type">A syntax node representing an interface type.</param>
        IObjectTypeDescriptor Interface(NamedTypeNode type);

        /// <summary>
        /// Specifies a delegate that can determine if a resolver result
        /// represents an object instance of this <see cref="ObjectType"/>.
        /// </summary>
        /// <param name="isOfType">
        /// The delegate that provides the IsInstanceOfType functionality.
        /// </param>
        IObjectTypeDescriptor IsOfType(IsOfType isOfType);

        /// <summary>
        /// Specifies an object type field.
        /// </summary>
        /// <param name="name">
        /// The name that the field shall have.
        /// </param>
        IFieldDescriptor Field(string name);
    }

    public interface IObjectTypeDescriptor<T>
        : IObjectTypeDescriptor
    {
        /// <summary>
        /// Defines the name of the <see cref="ObjectType"/>.
        /// </summary>
        /// <param name="name">The object type name.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="name"/> is <c>null</c> or <see cref="string.Empty"/>.
        /// </exception>
        new IObjectTypeDescriptor<T> Name(string name);

        /// <summary>
        /// Adds explanatory text of the <see cref="ObjectType"/>
        /// that can be accessd via introspection.
        /// </summary>
        /// <param name="description">The object type description.</param>
        new IObjectTypeDescriptor<T> Description(string description);

        /// <summary>
        /// Defines the field binding behavior.
        /// 
        /// The default binding behaviour is set to <see cref="BindingBehavior.Implicit"/>.
        /// </summary>
        /// <param name="bindingBehavior">
        /// The binding behavior.
        ///
        /// Implicit:
        /// The object type descriptor will try to infer the object type fields from the
        /// specified .net object type representation (<typeparamref name="T"/>).
        ///
        /// Explicit:
        /// All field have to specified explicitly via 
        /// <see cref="IObjectTypeDescriptor{T}.Field{TValue}(Expression{Func{T, TValue}})"/>
        /// or <see cref="IObjectTypeDescriptor.Field(string)"/>.
        /// </param>
        IObjectTypeDescriptor<T> BindFields(BindingBehavior bindingBehavior);

        /// <summary>
        /// Specifies an interface that is implemented by the <see cref="ObjectType"/>.
        /// </summary>
        /// <typeparam name="T">The interface type.</typeparam>
        new IObjectTypeDescriptor<T> Interface<TInterface>()
            where TInterface : InterfaceType;

        /// <summary>
        /// Specifies a delegate that can determine if a resolver result
        /// represents an object instance of this <see cref="ObjectType"/>.
        /// </summary>
        /// <param name="isOfType">
        /// The delegate that provides the IsInstanceOfType functionality.
        /// </param>
        new IObjectTypeDescriptor<T> IsOfType(IsOfType isOfType);

        /// <summary>
        /// Specifies an object type field.
        /// </summary>
        /// <param name="propertyOrMethod">
        /// An expression selecting a property or method of
        /// <typeparamref name="T"/> that shall represent a
        /// <see cref="ObjectType"/>.
        /// </param>
        IFieldDescriptor Field<TValue>(Expression<Func<T, TValue>> propertyOrMethod);
    }
}
