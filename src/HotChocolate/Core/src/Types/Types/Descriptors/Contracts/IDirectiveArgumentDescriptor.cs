using System;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types
{
    /// <summary>
    /// A fluent configuration API for GraphQL directive arguments.
    /// </summary>
    public interface IDirectiveArgumentDescriptor
        : IDescriptor<DirectiveArgumentDefinition>
        , IFluent
    {
        /// <summary>
        /// Associates the argument with a syntax node
        /// of the parsed GraphQL SDL.
        /// </summary>
        /// <param name="inputValueDefinition">
        /// The the type definition node.
        /// </param>
        IDirectiveArgumentDescriptor SyntaxNode(InputValueDefinitionNode inputValueDefinition);

        /// <summary>
        /// Defines the name of the <see cref="Argument"/>.
        /// </summary>
        /// <param name="value">The argument name.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="value"/> is <c>null</c> or
        /// <see cref="string.Empty"/>.
        /// </exception>
        IDirectiveArgumentDescriptor Name(NameString value);

        /// <summary>
        /// Adds explanatory text to the <see cref="Argument"/>
        /// that can be accessed via introspection.
        /// </summary>
        /// <param name="value">The argument description.</param>
        IDirectiveArgumentDescriptor Description(string value);

        /// <summary>
        /// Deprecates the argument.
        /// </summary>
        /// <param name="reason">The reason why this field is deprecated.</param>
        IDirectiveArgumentDescriptor Deprecated(string reason);

        /// <summary>
        /// Deprecates the argument.
        /// </summary>
        IDirectiveArgumentDescriptor Deprecated();

        /// <summary>
        /// Defines the type of the argument.
        /// </summary>
        /// <typeparam name="TInputType">
        /// The type.
        /// </typeparam>
        IDirectiveArgumentDescriptor Type<TInputType>()
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
        IDirectiveArgumentDescriptor Type<TInputType>(TInputType inputType)
            where TInputType : class, IInputType;

        /// <summary>
        /// Defines the type of the argument.
        /// </summary>
        IDirectiveArgumentDescriptor Type(ITypeNode typeNode);

        /// <summary>
        /// Defines the type of the argument.
        /// </summary>
        IDirectiveArgumentDescriptor Type(Type type);

        /// <summary>
        /// Sets the default value of the argument
        /// </summary>
        IDirectiveArgumentDescriptor DefaultValue(IValueNode value);

        /// <summary>
        /// Sets the default value of the argument
        /// </summary>
        IDirectiveArgumentDescriptor DefaultValue(object value);

        /// <summary>
        /// Ignores the argument of the directive
        /// </summary>
        /// <param name="ignore">
        /// when true the argument is ignored, when false the argument is not ignored
        /// </param>
        IDirectiveArgumentDescriptor Ignore(bool ignore = true);
    }
}
