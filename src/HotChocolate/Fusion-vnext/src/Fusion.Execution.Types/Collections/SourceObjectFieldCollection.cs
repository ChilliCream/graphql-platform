namespace HotChocolate.Fusion.Types.Collections;

public class SourceObjectFieldCollection(IEnumerable<SourceOutputField> members)
    : SourceMemberCollection<SourceOutputField>(members);
