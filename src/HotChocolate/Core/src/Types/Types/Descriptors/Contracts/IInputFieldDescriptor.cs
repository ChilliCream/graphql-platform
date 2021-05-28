using System;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types
{
    public interface IInputFieldDescriptor
        : IInputMemberDescriptor<IInputFieldDescriptor, InputFieldDefinition>
        , IDirectiveDescriptor<IInputFieldDescriptor>
        , IDefaultValueDescriptor<IInputFieldDescriptor>
    {
        /// <summary>
        /// Associates the specified <paramref name="inputValueDefinitionNode"/>
        /// with the <see cref="InputField"/>.
        /// </summary>
        /// <param name="inputValueDefinitionNode">
        /// The <see cref="InputValueDefinitionNode"/> of a parsed schema.
        /// </param>
        IInputFieldDescriptor SyntaxNode(
            InputValueDefinitionNode inputValueDefinitionNode);

        IInputFieldDescriptor Ignore(bool ignore = true);
    }
}
