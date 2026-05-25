using HotChocolate.Features;
using HotChocolate.Fusion.Features;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Extensions;

internal static class InputValueDefinitionExtensions
{
    extension(IInputValueDefinition inputValue)
    {
        public bool HasIsDirective
            => inputValue.Features.GetRequired<SourceInputFieldMetadata>().HasIsDirective;

        public bool HasRequireDirective
            => inputValue.Features.GetRequired<SourceInputFieldMetadata>().HasRequireDirective;

        /// <summary>
        /// Gets a value indicating whether the input field or its declaring member is inaccessible.
        /// </summary>
        public bool IsInaccessible
            => inputValue.Features.GetRequired<SourceInputFieldMetadata>().IsInaccessible;

        public IsInfo? IsInfo
            => inputValue.Features.GetRequired<SourceInputFieldMetadata>().IsInfo;

        public RequireInfo? RequireInfo
            => inputValue.Features.GetRequired<SourceInputFieldMetadata>().RequireInfo;
    }
}
