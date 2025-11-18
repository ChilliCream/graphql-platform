using HotChocolate.Features;
using HotChocolate.Fusion.Features;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Extensions;

internal static class EnumValueExtensions
{
    extension(IEnumValue enumValue)
    {
        /// <summary>
        /// Gets a value indicating whether the enum value or its declaring type is marked as
        /// inaccessible.
        /// </summary>
        public bool IsInaccessible
            => enumValue.Features.GetRequired<SourceEnumValueMetadata>().IsInaccessible;
    }
}
