using System;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types
{
    public interface IEnumValueDescriptor
        : IDescriptor<EnumValueDefinition>
        , IFluent
    {
        IEnumValueDescriptor SyntaxNode(
            EnumValueDefinitionNode enumValueDefinition);

        IEnumValueDescriptor Name(NameString value);

        IEnumValueDescriptor Description(string value);

        [Obsolete("Use `Deprecated`.")]
        IEnumValueDescriptor DeprecationReason(
            string reason);

        IEnumValueDescriptor Deprecated(string reason);

        IEnumValueDescriptor Deprecated();

        IEnumValueDescriptor Directive<T>(
            T directiveInstance)
            where T : class;

        IEnumValueDescriptor Directive<T>()
            where T : class, new();

        IEnumValueDescriptor Directive(
            NameString name,
            params ArgumentNode[] arguments);
    }
}
