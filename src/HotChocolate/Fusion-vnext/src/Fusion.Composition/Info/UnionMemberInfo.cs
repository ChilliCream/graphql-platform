using HotChocolate.Skimmed;

namespace HotChocolate.Fusion.Info;

internal record UnionMemberInfo(
    ObjectTypeDefinition MemberType,
    UnionTypeDefinition UnionType,
    SchemaDefinition Schema);
