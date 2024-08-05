namespace HotChocolate.Fusion.Planning.Collections;

public class SourceInterfaceMemberCollection(IEnumerable<SourceInterfaceField> members)
    : SourceMemberCollection<SourceInterfaceField>(members);
