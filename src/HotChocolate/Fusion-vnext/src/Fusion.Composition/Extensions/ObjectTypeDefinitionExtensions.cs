using HotChocolate.Features;
using HotChocolate.Fusion.Features;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Extensions;

internal static class ObjectTypeDefinitionExtensions
{
    extension(IObjectTypeDefinition objectType)
    {
        public bool HasShareableDirective
            => objectType.Features.GetRequired<SourceObjectTypeMetadata>().HasShareableDirective;

        public bool IsInternal
            => objectType.Features.GetRequired<SourceObjectTypeMetadata>().IsInternal;
    }
}
