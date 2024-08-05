namespace HotChocolate.Fusion.Planning.Collections;

public class SourceObjectTypeCollection(IEnumerable<SourceObjectType> members)
    : SourceMemberCollection<SourceObjectType>(members);
