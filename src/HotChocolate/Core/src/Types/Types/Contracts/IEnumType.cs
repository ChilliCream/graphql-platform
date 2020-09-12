using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Language;

#nullable enable

namespace HotChocolate.Types
{
    public interface IEnumType : ILeafType
    {
        new EnumTypeDefinitionNode? SyntaxNode { get; }

        IReadOnlyCollection<IEnumValue> Values { get; }

        bool TryGetRuntimeValue(
            NameString name,
            [NotNullWhen(true)]out object? runtimeValue);
    }
}
