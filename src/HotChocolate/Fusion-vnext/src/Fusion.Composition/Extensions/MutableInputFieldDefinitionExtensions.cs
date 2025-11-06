using HotChocolate.Features;
using HotChocolate.Fusion.Features;
using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion.Extensions;

internal static class MutableInputFieldDefinitionExtensions
{
    extension(MutableInputFieldDefinition inputField)
    {
        public bool HasIsDirective
            => inputField.Features.GetRequired<SourceInputFieldMetadata>().HasIsDirective;

        public bool HasRequireDirective
            => inputField.Features.GetRequired<SourceInputFieldMetadata>().HasRequireDirective;

        /// <summary>
        /// Gets a value indicating whether the input field or its declaring member is inaccessible.
        /// </summary>
        public bool IsInaccessible
            => inputField.Features.GetRequired<SourceInputFieldMetadata>().IsInaccessible;

        public IsInfo? IsInfo
            => inputField.Features.GetRequired<SourceInputFieldMetadata>().IsInfo;

        public RequireInfo? RequireInfo
            => inputField.Features.GetRequired<SourceInputFieldMetadata>().RequireInfo;
    }
}
