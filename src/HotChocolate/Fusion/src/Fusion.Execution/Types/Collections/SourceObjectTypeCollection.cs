namespace HotChocolate.Fusion.Types.Collections;

public class SourceObjectTypeCollection(IEnumerable<SourceObjectType> members)
    : SourceMemberCollection<SourceObjectType>(members);
