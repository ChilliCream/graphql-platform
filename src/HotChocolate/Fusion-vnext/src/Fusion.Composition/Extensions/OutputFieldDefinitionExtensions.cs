using System.Collections.Immutable;
using HotChocolate.Features;
using HotChocolate.Fusion.Features;
using HotChocolate.Types;
using static HotChocolate.Fusion.WellKnownArgumentNames;
using static HotChocolate.Fusion.WellKnownDirectiveNames;

namespace HotChocolate.Fusion.Extensions;

internal static class OutputFieldDefinitionExtensions
{
    extension(IOutputFieldDefinition outputField)
    {
        public ImmutableArray<string> GetSchemaNames(string? first = null)
        {
            var fusionFieldDirectives =
                outputField.Directives.AsEnumerable().Where(d => d.Name == FusionField);

            var schemaNames =
                fusionFieldDirectives.Select(d => (string)d.Arguments[Schema].Value!).ToList();

            if (first is not null && schemaNames.Contains(first))
            {
                schemaNames = schemaNames.Where(n => n != first).Prepend(first).ToList();
            }

            return [.. schemaNames];
        }

        public bool HasExternalDirective
            => outputField.Features.GetRequired<SourceOutputFieldMetadata>().HasExternalDirective;

        public bool HasInternalDirective
            => outputField.Features.GetRequired<SourceOutputFieldMetadata>().HasInternalDirective;

        public bool HasOverrideDirective
            => outputField.Features.GetRequired<SourceOutputFieldMetadata>().HasOverrideDirective;

        public bool HasProvidesDirective
            => outputField.Features.GetRequired<SourceOutputFieldMetadata>().HasProvidesDirective;

        public bool HasShareableDirective
            => outputField.Features.GetRequired<SourceOutputFieldMetadata>().HasShareableDirective;

        public bool IsExternal
            => outputField.Features.GetRequired<SourceOutputFieldMetadata>().IsExternal;

        /// <summary>
        /// Gets a value indicating whether the field or its declaring type is marked as inaccessible.
        /// </summary>
        public bool IsInaccessible
            => outputField.Features.GetRequired<SourceOutputFieldMetadata>().IsInaccessible;

        /// <summary>
        /// Gets a value indicating whether the field or its declaring type is marked as internal.
        /// </summary>
        public bool IsInternal
            => outputField.Features.GetRequired<SourceOutputFieldMetadata>().IsInternal;

        public bool IsLookup
            => outputField.Features.GetRequired<SourceOutputFieldMetadata>().IsLookup;

        public bool IsOverridden
            => outputField.Features.GetRequired<SourceOutputFieldMetadata>().IsOverridden;

        public bool IsShareable
            => outputField.Features.GetRequired<SourceOutputFieldMetadata>().IsShareable;

        public ProvidesInfo? ProvidesInfo
            => outputField.Features.GetRequired<SourceOutputFieldMetadata>().ProvidesInfo;
    }
}
