namespace HotChocolate.Fusion.Types.Collections;

public class SourceInterfaceMemberCollection(IEnumerable<SourceInterfaceField> members)
    : SourceMemberCollection<SourceInterfaceField>(members);
