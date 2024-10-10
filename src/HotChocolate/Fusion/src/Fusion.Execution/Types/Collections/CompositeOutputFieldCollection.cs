namespace HotChocolate.Fusion.Types.Collections;

public sealed class CompositeOutputFieldCollection(
    IEnumerable<CompositeOutputField> fields)
    : CompositeFieldCollection<CompositeOutputField>(fields);
