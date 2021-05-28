using System;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types
{
    public interface IDirectiveArgumentDescriptor
        : IInputMemberDescriptor<IDirectiveArgumentDescriptor, DirectiveArgumentDefinition>
        , IDefaultValueDescriptor<IDirectiveArgumentDescriptor>
    {
        /// <summary>
        /// Associates the argument with a syntax node
        /// of the parsed GraphQL SDL.
        /// </summary>
        /// <param name="inputValueDefinition">
        /// The the type definition node.
        /// </param>
        IDirectiveArgumentDescriptor SyntaxNode(
            InputValueDefinitionNode inputValueDefinition);

        IDirectiveArgumentDescriptor Ignore(bool ignore = true);
    }

    public interface IMemberDescriptor<out TDescriptor>
        : IFluent
        where TDescriptor : IDescriptor
    {
        /// <summary>
        /// Defines the name of the member.
        /// </summary>
        /// <param name="value">The name.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="value"/> is <c>null</c> or
        /// <see cref="string.Empty"/>.
        /// </exception>
        TDescriptor Name(NameString value);

        /// <summary>
        /// Adds explanatory text to the member
        /// that can be accessed via introspection.
        /// </summary>
        /// <param name="value">The explanatory text.</param>
        TDescriptor Description(string value);

        /// <summary>
        /// Defines the type of this member.
        /// </summary>
        TDescriptor Type(Type type);

        /// <summary>
        /// Defines the type of this member with parsed GraphQL SDL type syntax.
        /// </summary>
        TDescriptor Type(ITypeNode typeNode);
    }

    public interface IOutputMemberDescriptor<out TDescriptor>
        : IMemberDescriptor<TDescriptor>
        where TDescriptor : IDescriptor
    {
        /// <summary>
        /// Defines the return type of the member.
        /// </summary>
        /// <typeparam name="T">
        /// The GraphQL output type.
        /// </typeparam>
        IObjectFieldDescriptor Type<T>() where T : class, IOutputType;

        /// <summary>
        /// Defines the type of the object field.
        /// </summary>
        /// <typeparam name="T">
        /// The GraphQL output type.
        /// </typeparam>
        /// <param name="outputType">
        /// The GraphQL output type instance.
        /// </param>
        IObjectFieldDescriptor Type<T>(T outputType) where T : class, IOutputType;
    }

    public interface IInputMemberDescriptor<out TDescriptor>
        : IMemberDescriptor<TDescriptor>
        where TDescriptor : IDescriptor
    {
        /// <summary>
        /// Defines the return type of the member.
        /// </summary>
        /// <typeparam name="T">
        /// The GraphQL input type.
        /// </typeparam>
        IObjectFieldDescriptor Type<T>() where T : class, IInputType;

        /// <summary>
        /// Defines the type of the object field.
        /// </summary>
        /// <typeparam name="T">
        /// The GraphQL output type.
        /// </typeparam>
        /// <param name="inputType">
        /// The GraphQL input type instance.
        /// </param>
        IObjectFieldDescriptor Type<T>(T inputType) where T : class, IInputType;
    }

    public interface IOutputMemberDescriptor<out TDescriptor, TDefinition>
        : IOutputMemberDescriptor<TDescriptor>
        , IDescriptor<TDefinition>
        where TDescriptor : IDescriptor<TDefinition>
        where TDefinition : DefinitionBase
    {
    }

    public interface IInputMemberDescriptor<out TDescriptor, TDefinition>
        : IInputMemberDescriptor<TDescriptor>
        , IDescriptor<TDefinition>
        where TDescriptor : IDescriptor<TDefinition>
        where TDefinition : DefinitionBase
    {
    }

    public interface IDirectiveDescriptor<out TDescriptor>
        : IFluent
        where TDescriptor : IDescriptor
    {
        TDescriptor Directive<T>(T directiveInstance) where T : class;

        TDescriptor Directive<T>() where T : class, new();

        TDescriptor Directive(NameString name, params ArgumentNode[] arguments);
    }

    public interface IDefaultValueDescriptor<out TDescriptor>
        : IFluent
        where TDescriptor : IDescriptor
    {
        TDescriptor DefaultValue(IValueNode value);

        TDescriptor DefaultValue(object value);
    }
}
