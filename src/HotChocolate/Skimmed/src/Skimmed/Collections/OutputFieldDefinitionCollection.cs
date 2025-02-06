using HotChocolate.Types;

namespace HotChocolate.Skimmed;

public sealed class OutputFieldDefinitionCollection
    : FieldDefinitionCollection<OutputFieldDefinition>
    , IOutputFieldDefinitionCollection
    , IReadOnlyFieldDefinitionCollection<OutputFieldDefinition>;
