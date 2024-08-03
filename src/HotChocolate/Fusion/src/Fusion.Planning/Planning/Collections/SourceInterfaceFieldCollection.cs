namespace HotChocolate.Fusion.Planning.Collections;

public class SourceInterfaceFieldCollection(IEnumerable<SourceInterfaceField> fields)
    : SourceFieldCollection<SourceInterfaceField>(fields);
