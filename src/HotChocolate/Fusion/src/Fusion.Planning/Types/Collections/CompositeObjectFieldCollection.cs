namespace HotChocolate.Fusion.Types.Collections;

public sealed class CompositeObjectFieldCollection(
    IEnumerable<CompositeObjectField> fields)
    : CompositeFieldCollection<CompositeObjectField>(fields);
