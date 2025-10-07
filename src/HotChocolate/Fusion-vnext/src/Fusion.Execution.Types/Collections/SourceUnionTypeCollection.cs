using HotChocolate.Fusion.Types.Metadata;

namespace HotChocolate.Fusion.Types.Collections;

public class SourceUnionTypeCollection(IEnumerable<SourceUnionType> members)
    : SourceMemberCollection<SourceUnionType>(members)
{
}
