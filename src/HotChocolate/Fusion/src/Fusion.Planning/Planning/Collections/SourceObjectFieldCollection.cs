namespace HotChocolate.Fusion.Planning.Collections;

public class SourceObjectFieldCollection(IEnumerable<SourceObjectField> members)
    : SourceMemberCollection<SourceObjectField>(members);
