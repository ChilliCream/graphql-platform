namespace HotChocolate.Fusion.Planning.Collections;

public sealed class CompositeInterfaceFieldCollection(
    IEnumerable<CompositeInterfaceField> fields)
    : CompositeFieldCollection<CompositeInterfaceField>(fields);
