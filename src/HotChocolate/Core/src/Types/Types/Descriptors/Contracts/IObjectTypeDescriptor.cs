using System;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types
{
    public interface IObjectTypeDescriptor
        : IDescriptor<ObjectTypeDefinition>
        , IFluent
    {
        /// <summary>
        /// Associates the specified <paramref name="objectTypeDefinition"/>
        /// with the <see cref="ObjectType"/>.
        /// </summary>
        /// <param name="objectTypeDefinition">
        /// The <see cref="ObjectTypeDefinitionNode"/> of a parsed schema.
        /// </param>
        IObjectTypeDescriptor SyntaxNode(
            ObjectTypeDefinitionNode objectTypeDefinition);

        /// <summary>
        /// Defines the name of the <see cref="ObjectType"/>.
        /// </summary>
        /// <param name="value">The object type name.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="value"/> is <c>null</c> or
        /// <see cref="string.Empty"/>.
        /// </exception>
        IObjectTypeDescriptor Name(NameString value);

        /// <summary>
        /// Adds explanatory text to the <see cref="ObjectType"/>
        /// that can be accessed via introspection.
        /// </summary>
        /// <param name="value">The object type description.</param>
        IObjectTypeDescriptor Description(string value);

        /// <summary>
        /// Specifies an interface that is implemented by the
        /// <see cref="ObjectType"/>.
        /// </summary>
        /// <typeparam name="T">The interface type.</typeparam>
        IObjectTypeDescriptor Interface<T>()
            where T : InterfaceType;

        /// <summary>
        /// Specifies an interface that is implemented by the
        /// <see cref="ObjectType"/>.
        /// </summary>
        /// <typeparam name="T">The interface type.</typeparam>
        IObjectTypeDescriptor Interface<T>(T type)
            where T : InterfaceType;

        /// <summary>
        /// Specifies an interface that is implemented by the
        /// <see cref="ObjectType"/>.
        /// </summary>
        /// <param name="type">
        /// A syntax node representing an interface type.
        /// </param>
        IObjectTypeDescriptor Interface(NamedTypeNode type);

        /// <summary>
        /// Specifies an interface that is implemented by the
        /// <see cref="ObjectType"/>.
        /// </summary>
        /// <typeparam name="T">The interface type.</typeparam>
        IObjectTypeDescriptor Implements<T>()
            where T : InterfaceType;

        /// <summary>
        /// Specifies an interface that is implemented by the
        /// <see cref="ObjectType"/>.
        /// </summary>
        /// <typeparam name="T">The interface type.</typeparam>
        IObjectTypeDescriptor Implements<T>(T type)
            where T : InterfaceType;

        /// <summary>
        /// Specifies an interface that is implemented by the
        /// <see cref="ObjectType"/>.
        /// </summary>
        /// <param name="type">
        /// A syntax node representing an interface type.
        /// </param>
        IObjectTypeDescriptor Implements(NamedTypeNode type);

        /// <summary>
        /// Includes a resolver type and imports all the methods and
        /// fields from it.
        /// </summary>
        /// <typeparam name="TResolver">A resolver type.</typeparam>
        IObjectTypeDescriptor Include<TResolver>();

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
        /// The member that shall be used as a field.
        /// </param>
        IObjectFieldDescriptor Field(MemberInfo propertyOrMethod);

        IObjectTypeDescriptor Directive<T>(T directiveInstance)
            where T : class;

        IObjectTypeDescriptor Directive<T>()
            where T : class, new();

        IObjectTypeDescriptor Directive(
            NameString name,
            params ArgumentNode[] arguments);
    }
}
