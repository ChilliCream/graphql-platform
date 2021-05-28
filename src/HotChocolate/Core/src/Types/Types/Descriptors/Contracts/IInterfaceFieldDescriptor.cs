using System;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types
{
    public interface IInterfaceFieldDescriptor
        : IOutputMemberDescriptor<IInterfaceFieldDescriptor, InterfaceFieldDefinition>
        , IDirectiveDescriptor<IInterfaceFieldDescriptor>
    {
        IInterfaceFieldDescriptor SyntaxNode(
            FieldDefinitionNode fieldDefinitionNode);

        [Obsolete("Use `Deprecated`.")]
        IInterfaceFieldDescriptor DeprecationReason(
            string reason);

        IInterfaceFieldDescriptor Deprecated(string reason);

        IInterfaceFieldDescriptor Deprecated();

        IInterfaceFieldDescriptor Ignore(bool ignore = true);

        IInterfaceFieldDescriptor Argument(
            NameString name,
            Action<IArgumentDescriptor> argument);
    }
}
