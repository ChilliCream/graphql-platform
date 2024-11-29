using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Definitions;

#nullable enable

namespace HotChocolate.Types;

/// <summary>
/// A fluent configuration API for GraphQL object types.
/// </summary>
/// <typeparam name="TRuntimeType">
/// The runtime type.
/// </typeparam>
public interface IObjectTypeDescriptor<TRuntimeType>
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
    IObjectTypeDescriptor<TRuntimeType> Name(string value);

    /// <summary>
    /// Adds explanatory text of the <see cref="ObjectType"/>
    /// that can be accessed via introspection.
    /// </summary>
    /// <param name="value">The object type description.</param>
    IObjectTypeDescriptor<TRuntimeType> Description(string? value);

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
    /// (<typeparamref name="TRuntimeType"/>).
    ///
    /// Explicit:
    /// All field have to be specified explicitly via
    /// <see cref="Field(Expression{Func{TRuntimeType, object}})"/>
    /// or <see cref="Field(string)"/>.
    /// </param>
    IObjectTypeDescriptor<TRuntimeType> BindFields(BindingBehavior behavior);

    /// <summary>
    /// Defines from which runtime member the GraphQL type shall infer its fields.
    /// </summary>
    /// <param name="bindingFlags">
    /// The binding flags.
    /// </param>
    IObjectTypeDescriptor<TRuntimeType> BindFields(FieldBindingFlags bindingFlags);

    /// <summary>
    /// Defines that all fields have to be specified explicitly.
    /// </summary>
    IObjectTypeDescriptor<TRuntimeType> BindFieldsExplicitly();

    /// <summary>
    /// Defines that all fields shall be inferred
    /// from the associated .Net type,
    /// </summary>
    IObjectTypeDescriptor<TRuntimeType> BindFieldsImplicitly();

    /// <summary>
    /// Specifies an interface that is implemented by the
    /// <see cref="ObjectType"/>.
    /// </summary>
    /// <typeparam name="TInterface">The interface type.</typeparam>
    IObjectTypeDescriptor<TRuntimeType> Implements<TInterface>()
        where TInterface : InterfaceType;

    /// <summary>
    /// Specifies an interface that is implemented by the
    /// <see cref="ObjectType"/>.
    /// </summary>
    /// <typeparam name="TInterface">The interface type.</typeparam>
    IObjectTypeDescriptor<TRuntimeType> Implements<TInterface>(TInterface type)
        where TInterface : InterfaceType;

    /// <summary>
    /// Specifies an interface that is implemented by the
    /// <see cref="ObjectType"/>.
    /// </summary>
    /// <param name="type">
    /// A syntax node representing an interface type.
    /// </param>
    IObjectTypeDescriptor<TRuntimeType> Implements(NamedTypeNode type);

    /// <summary>
    /// Specifies a delegate that can determine if a resolver result
    /// represents an object instance of this <see cref="ObjectType"/>.
    /// </summary>
    /// <param name="isOfType">
    /// The delegate that provides the IsInstanceOfType functionality.
    /// </param>
    IObjectTypeDescriptor<TRuntimeType> IsOfType(IsOfType isOfType);

    /// <summary>
    /// Specifies an object type field.
    /// </summary>
    /// <param name="propertyOrMethod">
    /// An expression selecting a property or method of
    /// <typeparamref name="TRuntimeType"/>.
    /// </param>
    IObjectFieldDescriptor Field(
        Expression<Func<TRuntimeType, object?>> propertyOrMethod);

    /// <summary>
    /// Specifies an object type field.
    /// </summary>
    /// <param name="propertyOrMethod">
    /// An expression selecting a property or method of
    /// <typeparamref name="TRuntimeType"/>.
    /// </param>
    IObjectFieldDescriptor Field<TValue>(
        Expression<Func<TRuntimeType, TValue?>> propertyOrMethod);

    /// <summary>
    /// Specifies an object type field.
    /// </summary>
    /// <param name="name">
    /// The name that the field shall have.
    /// </param>
    IObjectFieldDescriptor Field(string name);

    /// <summary>
    /// Specifies an object type field which is bound to a resolver type.
    /// </summary>
    /// <param name="propertyOrMethod">
    /// An expression selecting a property or method of
    /// <typeparamref name="TResolver"/>.
    /// The resolver type containing the property or method.
    /// </param>
    IObjectFieldDescriptor Field<TResolver>(
        Expression<Func<TResolver, object?>> propertyOrMethod);

    /// <summary>
    /// Specifies an object type field which is bound to a resolver type.
    /// </summary>
    /// <param name="propertyOrMethod">
    /// The member representing a field.
    /// </param>
    IObjectFieldDescriptor Field(MemberInfo propertyOrMethod);

    IObjectTypeDescriptor<TRuntimeType> Directive<TDirective>(
        TDirective directiveInstance)
        where TDirective : class;

    IObjectTypeDescriptor<TRuntimeType> Directive<TDirective>()
        where TDirective : class, new();

    IObjectTypeDescriptor<TRuntimeType> Directive(
        string name,
        params ArgumentNode[] arguments);

    /// <summary>
    /// If configuring a type extension this is the type that shall be extended.
    /// </summary>
    /// <param name="extendsType">
    /// The type to extend.
    /// </param>
    IObjectTypeDescriptor ExtendsType(Type extendsType);

    /// <summary>
    /// If configuring a type extension this is the type that shall be extended.
    /// </summary>
    /// <typeparam name="TExtendsType">The type to extend.</typeparam>
    IObjectTypeDescriptor ExtendsType<TExtendsType>();
}
