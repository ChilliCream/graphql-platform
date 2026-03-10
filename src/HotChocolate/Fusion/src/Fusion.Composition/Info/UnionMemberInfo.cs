using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion.Info;

internal record UnionMemberInfo(
    MutableObjectTypeDefinition MemberType,
    MutableUnionTypeDefinition UnionType,
    MutableSchemaDefinition Schema);
