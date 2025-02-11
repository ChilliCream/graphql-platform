using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion.Info;

internal record UnionMemberInfo(
    ObjectTypeDefinition MemberType,
    UnionTypeDefinition UnionType,
    SchemaDefinition Schema);
