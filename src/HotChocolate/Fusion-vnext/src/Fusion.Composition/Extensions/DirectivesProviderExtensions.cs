using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using ArgumentNames = HotChocolate.Fusion.WellKnownArgumentNames;

namespace HotChocolate.Fusion.Extensions;

internal static class DirectivesProviderExtensions
{
    extension(IDirectivesProvider member)
    {
        public void AddDirective(Directive directive)
        {
            switch (member)
            {
                case IMutableFieldDefinition field:
                    field.Directives.Add(directive);
                    break;
                case IMutableTypeDefinition type:
                    type.Directives.Add(directive);
                    break;
                case MutableEnumValue enumValue:
                    enumValue.Directives.Add(directive);
                    break;
                case MutableSchemaDefinition schema:
                    schema.Directives.Add(directive);
                    break;
                default:
                    throw new InvalidOperationException();
            }
        }

        public string? GetIsFieldSelectionMap()
        {
            var isDirective = member.Directives.FirstOrDefault(d => d.Name == WellKnownDirectiveNames.Is);

            if (isDirective?.Arguments[ArgumentNames.Field] is StringValueNode fieldArgument)
            {
                return fieldArgument.Value;
            }

            return null;
        }

        public string? GetProvidesSelectionSet()
        {
            var providesDirective =
                member.Directives.FirstOrDefault(d => d.Name == WellKnownDirectiveNames.Provides);

            if (providesDirective?.Arguments[ArgumentNames.Fields] is StringValueNode fieldsArgument)
            {
                return fieldsArgument.Value;
            }

            return null;
        }

        public HashSet<string> GetTags()
        {
            var tags = new HashSet<string>();
            var tagDirectives = member.Directives.Where(d => d.Name == WellKnownDirectiveNames.Tag);

            foreach (var tagDirective in tagDirectives)
            {
                if (tagDirective.Arguments[ArgumentNames.Name] is StringValueNode name)
                {
                    tags.Add(name.Value);
                }
            }

            return tags;
        }

        public bool ExistsInSchema(string schemaName)
        {
            return member.Directives.AsEnumerable().Any(
                d =>
                    d.Name == WellKnownDirectiveNames.FusionType
                    && (string)d.Arguments[ArgumentNames.Schema].Value! == schemaName);
        }

        public bool HasFusionInaccessibleDirective()
        {
            return member.Directives.ContainsName(WellKnownDirectiveNames.FusionInaccessible);
        }

        public bool HasInaccessibleDirective()
        {
            return member.Directives.ContainsName(WellKnownDirectiveNames.Inaccessible);
        }
    }
}
