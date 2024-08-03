namespace HotChocolate.Fusion.Planning.Collections;

public sealed class CompositeObjectFieldCollection(
    IEnumerable<CompositeObjectField> fields)
    : CompositeFieldCollection<CompositeObjectField>(fields);
