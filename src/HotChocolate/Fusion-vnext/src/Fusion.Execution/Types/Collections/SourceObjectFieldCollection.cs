namespace HotChocolate.Fusion.Types.Collections;

public class SourceObjectFieldCollection(IEnumerable<SourceObjectField> members)
    : SourceMemberCollection<SourceObjectField>(members);
