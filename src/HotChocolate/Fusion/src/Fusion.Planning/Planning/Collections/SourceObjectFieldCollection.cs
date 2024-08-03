namespace HotChocolate.Fusion.Planning.Collections;

public class SourceObjectFieldCollection(IEnumerable<SourceObjectField> fields)
    : SourceFieldCollection<SourceObjectField>(fields);
