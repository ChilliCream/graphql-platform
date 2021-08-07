using System;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types
{
    /// <summary>
    /// A fluent configuration API for GraphQL fields.
    /// </summary>
    public interface IInputFieldDescriptor
        : IDescriptor<InputFieldDefinition>
        , IFluent
    {
        /// <summary>
        /// Associates the input field with a syntax node
        /// of the parsed GraphQL SDL.
        /// </summary>
        /// <param name="inputValueDefinitionNode">
        /// The the type definition node.
        /// </param>
        IInputFieldDescriptor SyntaxNode(InputValueDefinitionNode inputValueDefinitionNode);

        /// <summary>
        /// Defines the name of the <see cref="InputField"/>.
        /// </summary>
        /// <param name="value">The input field name.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="value"/> is <c>null</c> or
        /// <see cref="string.Empty"/>.
        /// </exception>
        IInputFieldDescriptor Name(NameString value);

        /// <summary>
        /// Adds explanatory text to the <see cref="InputField"/>
        /// that can be accessed via introspection.
        /// </summary>
        /// <param name="value">The input field description.</param>
        IInputFieldDescriptor Description(string value);

        /// <summary>
        /// Deprecates the input field.
        /// </summary>
        /// <param name="reason">The reason why this field is deprecated.</param>
        IInputFieldDescriptor Deprecated(string reason);

        /// <summary>
        /// Deprecates the input field.
        /// </summary>
        IInputFieldDescriptor Deprecated();

        /// <summary>
        /// Defines the type of the input field.
        /// </summary>
        /// <typeparam name="TInputType">
        /// The type.
        /// </typeparam>
        IInputFieldDescriptor Type<TInputType>()
            where TInputType : IInputType;

        /// <summary>
        /// Defines the type of the input field.
        /// </summary>
        /// <typeparam name="TInputType">
        /// The type.
        /// </typeparam>
        /// <param name="inputType">
        /// The input type instance.
        /// </param>
        IInputFieldDescriptor Type<TInputType>(TInputType inputType)
            where TInputType : class, IInputType;

        /// <summary>
        /// Defines the type of the input field.
        /// </summary>
        IInputFieldDescriptor Type(ITypeNode typeNode);

        /// <summary>
        /// Defines the type of the input field.
        /// </summary>
        IInputFieldDescriptor Type(Type type);

        /// <summary>
        /// Ignores the field of the input type
        /// </summary>
        /// <param name="ignore">
        /// when true the field is ignored, when false the field is not ignored
        /// </param>
        IInputFieldDescriptor Ignore(bool ignore = true);

        /// <summary>
        /// Sets the default value of the input field
        /// </summary>
        IInputFieldDescriptor DefaultValue(IValueNode value);

        /// <summary>
        /// Sets the default value of the input field
        /// </summary>
        IInputFieldDescriptor DefaultValue(object value);

        /// <summary>
        /// Registers a directive on the field
        /// </summary>
        /// <param name="directiveInstance">The instance of the directive</param>
        /// <typeparam name="T">The type of the directive</typeparam>
        /// <returns>The descriptor</returns>
        IInputFieldDescriptor Directive<T>(T directiveInstance)
            where T : class;

        /// <summary>
        /// Registers a directive on the field
        /// </summary>
        /// <typeparam name="T">The type of the directive</typeparam>
        /// <returns>The descriptor</returns>
        IInputFieldDescriptor Directive<T>()
            where T : class, new();

        /// <summary>
        /// Registers a directive on the field
        /// </summary>
        /// <param name="name">The name of the directive</param>
        /// <param name="arguments">The arguments of the directive</param>
        /// <returns>The descriptor</returns>
        IInputFieldDescriptor Directive(
            NameString name,
            params ArgumentNode[] arguments);
    }
}
