namespace HotChocolate.Fusion.Types.Collections;

public sealed class CompositeInterfaceFieldCollection(
    IEnumerable<CompositeInterfaceField> fields)
    : CompositeFieldCollection<CompositeInterfaceField>(fields);
