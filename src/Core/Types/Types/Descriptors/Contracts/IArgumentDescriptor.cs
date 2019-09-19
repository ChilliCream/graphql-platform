using System;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types
{
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

        IArgumentDescriptor Description(string value);

        IArgumentDescriptor Type<TInputType>()
            where TInputType : IInputType;

        IArgumentDescriptor Type<TInputType>(TInputType inputType)
            where TInputType : class, IInputType;

        IArgumentDescriptor Type(ITypeNode typeNode);

        IArgumentDescriptor Type(Type type);

        IArgumentDescriptor DefaultValue(IValueNode value);

        IArgumentDescriptor DefaultValue(object value);

        IArgumentDescriptor Directive<T>(T directiveInstance)
            where T : class;

        IArgumentDescriptor Directive<T>()
            where T : class, new();

        IArgumentDescriptor Directive(NameString name, params ArgumentNode[] arguments);
    }
}
