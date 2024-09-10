#nullable enable

namespace HotChocolate.Types.Descriptors.Definitions;

public interface IComplexOutputTypeDefinition
{
    string Name { get; }

    Type RuntimeType { get; }

    IList<Type> KnownRuntimeTypes { get; }

    IList<TypeReference> Interfaces { get; }
}
