using System;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types
{
    /// <summary>
    /// A fluent configuration API for GraphQL arguments.
    /// </summary>
    public interface IArgumentDescriptor
        : IDescriptor<ArgumentDefinition>
        , IFluent
    {
        /// <summary>
        /// Associates the argument with a syntax node
        /// of the parsed GraphQL SDL.
        /// </summary>
        /// <param name="inputValueDefinition">
        /// The the type definition node.
        /// </param>
        IArgumentDescriptor SyntaxNode(InputValueDefinitionNode inputValueDefinition);

        /// <summary>
        /// Adds explanatory text to the <see cref="Argument"/>
        /// that can be accessed via introspection.
        /// </summary>
        /// <param name="value">The argument description.</param>
        IArgumentDescriptor Description(string value);

        /// <summary>
        /// Deprecates the argument.
        /// </summary>
        /// <param name="reason">The reason why this field is deprecated.</param>
        IArgumentDescriptor Deprecated(string reason);

        /// <summary>
        /// Deprecates the argument.
        /// </summary>
        IArgumentDescriptor Deprecated();

        /// <summary>
        /// Defines the type of the argument.
        /// </summary>
        /// <typeparam name="TInputType">
        /// The type.
        /// </typeparam>
        IArgumentDescriptor Type<TInputType>()
            where TInputType : IInputType;

        /// <summary>
        /// Defines the type of the argument.
        /// </summary>
        /// <typeparam name="TInputType">
        /// The type.
        /// </typeparam>
        /// <param name="inputType">
        /// The input type instance.
        /// </param>
        IArgumentDescriptor Type<TInputType>(TInputType inputType)
            where TInputType : class, IInputType;

        /// <summary>
        /// Defines the type of the argument.
        /// </summary>
        IArgumentDescriptor Type(ITypeNode typeNode);

        /// <summary>
        /// Defines the type of the argument.
        /// </summary>
        IArgumentDescriptor Type(Type type);

        /// <summary>
        /// Sets the default value of the argument
        /// </summary>
        IArgumentDescriptor DefaultValue(IValueNode value);

        /// <summary>
        /// Sets the default value of the argument
        /// </summary>
        IArgumentDescriptor DefaultValue(object value);

        /// <summary>
        /// Registers a directive on the field
        /// </summary>
        /// <param name="directiveInstance">The instance of the directive</param>
        /// <typeparam name="T">The type of the directive</typeparam>
        /// <returns>The descriptor</returns>
        IArgumentDescriptor Directive<T>(T directiveInstance)
            where T : class;

        /// <summary>
        /// Registers a directive on the field
        /// </summary>
        /// <typeparam name="T">The type of the directive</typeparam>
        /// <returns>The descriptor</returns>
        IArgumentDescriptor Directive<T>()
            where T : class, new();

        /// <summary>
        /// Registers a directive on the field
        /// </summary>
        /// <param name="name">The name of the directive</param>
        /// <param name="arguments">The arguments of the directive</param>
        /// <returns>The descriptor</returns>
        IArgumentDescriptor Directive(NameString name, params ArgumentNode[] arguments);
    }
}
