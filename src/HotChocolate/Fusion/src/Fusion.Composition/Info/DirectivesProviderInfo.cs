using HotChocolate.Types;
using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion.Info;

internal record DirectivesProviderInfo(IDirectivesProvider Member, MutableSchemaDefinition Schema);
