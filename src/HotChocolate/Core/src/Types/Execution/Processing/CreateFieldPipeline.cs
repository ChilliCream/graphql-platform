using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Execution.Processing;

internal delegate FieldDelegate CreateFieldPipeline(
    Schema schema,
    ObjectField field,
    FieldNode selection);
