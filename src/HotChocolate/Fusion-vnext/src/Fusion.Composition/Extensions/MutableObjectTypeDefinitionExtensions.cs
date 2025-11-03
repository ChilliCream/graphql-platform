using HotChocolate.Features;
using HotChocolate.Fusion.Features;
using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion.Extensions;

internal static class MutableObjectTypeDefinitionExtensions
{
    extension(MutableObjectTypeDefinition objectType)
    {
        public bool HasShareableDirective
            => objectType.Features.GetRequired<SourceObjectTypeMetadata>().HasShareableDirective;

        public bool IsInternal
            => objectType.Features.GetRequired<SourceObjectTypeMetadata>().IsInternal;
    }
}
