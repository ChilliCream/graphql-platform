using System.Collections.Immutable;
using HotChocolate.Fusion.Directives;
using HotChocolate.Fusion.Events;
using HotChocolate.Fusion.Events.Contracts;
using HotChocolate.Fusion.Extensions;
using HotChocolate.Fusion.Language;
using HotChocolate.Fusion.SchemaVisitors;
using HotChocolate.Fusion.Validators;
using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using static HotChocolate.Fusion.Logging.LogEntryHelper;
using static HotChocolate.Fusion.WellKnownArgumentNames;
using static HotChocolate.Fusion.WellKnownDirectiveNames;

namespace HotChocolate.Fusion.PostMergeValidationRules;

/// <summary>
/// Fails composition with <c>AUTHORIZATION_TRANSITIVE_REQUIREMENTS_MISSING</c> when a field reads
/// other fields through <c>@require</c> or a lookup key without declaring all of their
/// authorization requirements. A requirement is declared when every alternative group of it
/// contains a group of the dependency and <c>@authenticated</c> is present whenever the dependency
/// has it.
/// </summary>
internal sealed class AuthorizationTransitiveRequirementsRule : IEventHandler<SchemaEvent>
{
    public void Handle(SchemaEvent @event, CompositionContext context)
    {
        var schema = @event.Schema;

        if (!HasAuthorization(schema))
        {
            return;
        }

        var checker = new Checker(schema, context);

        foreach (var sourceSchema in context.SchemaDefinitions)
        {
            checker.CheckRequireDirectives(sourceSchema);
            checker.CheckLookupKeys(sourceSchema);
        }
    }

    private static bool HasAuthorization(MutableSchemaDefinition schema)
    {
        foreach (var type in schema.Types.OfType<MutableComplexTypeDefinition>())
        {
            foreach (var field in type.Fields)
            {
                if (field.Directives.ContainsName(FusionAuthorization))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private sealed class Checker(MutableSchemaDefinition schema, CompositionContext context)
    {
        private readonly FieldSelectionMapValidator _validator = new(schema);
        private readonly HashSet<(string Requiring, string Dependency)> _reported = [];

        public void CheckRequireDirectives(MutableSchemaDefinition sourceSchema)
        {
            foreach (var sourceType in sourceSchema.Types.OfType<MutableComplexTypeDefinition>())
            {
                if (!schema.Types.TryGetType<MutableComplexTypeDefinition>(
                        sourceType.Name,
                        out var mergedType))
                {
                    continue;
                }

                foreach (var sourceField in sourceType.Fields)
                {
                    if (!mergedType.Fields.TryGetField(sourceField.Name, out var mergedField))
                    {
                        continue;
                    }

                    foreach (var argument in sourceField.Arguments)
                    {
                        if (!argument.HasRequireDirective
                            || argument.Directives[Require].First().Arguments[Field].Value
                                is not string map)
                        {
                            continue;
                        }

                        var dependencies = SelectFields(map, argument, mergedType);

                        Check(
                            mergedField,
                            dependencies,
                            new SchemaCoordinate(sourceType.Name, sourceField.Name, argument.Name),
                            sourceSchema);
                    }
                }
            }
        }

        public void CheckLookupKeys(MutableSchemaDefinition sourceSchema)
        {
            var lookups = new DiscoverLookupsSchemaVisitor(sourceSchema).Discover();

            foreach (var (_, lookupGroup) in lookups)
            {
                foreach (var (lookupField, _, _) in lookupGroup)
                {
                    foreach (var sourceType in GetPossibleTypes(lookupField, sourceSchema))
                    {
                        if (schema.Types.TryGetType<MutableComplexTypeDefinition>(
                                sourceType.Name,
                                out var mergedType))
                        {
                            CheckLookupKey(lookupField, sourceType, mergedType, sourceSchema);
                        }
                    }
                }
            }
        }

        private static IEnumerable<MutableObjectTypeDefinition> GetPossibleTypes(
            MutableOutputFieldDefinition lookupField,
            MutableSchemaDefinition sourceSchema)
        {
            return lookupField.Type.AsTypeDefinition() switch
            {
                MutableObjectTypeDefinition objectType => [objectType],
                MutableInterfaceTypeDefinition interfaceType
                    => sourceSchema.GetPossibleTypes(interfaceType).ToArray(),
                MutableUnionTypeDefinition unionType
                    => sourceSchema.GetPossibleTypes(unionType).ToArray(),
                _ => []
            };
        }

        private void CheckLookupKey(
            MutableOutputFieldDefinition lookupField,
            MutableObjectTypeDefinition sourceType,
            MutableComplexTypeDefinition mergedType,
            MutableSchemaDefinition sourceSchema)
        {
            var dependencies = new List<IOutputFieldDefinition>();

            foreach (var argument in lookupField.Arguments)
            {
                if (!argument.HasRequireDirective)
                {
                    dependencies.AddRange(
                        SelectFields(
                            argument.GetIsFieldSelectionMap() ?? argument.Name,
                            argument,
                            mergedType));
                }
            }

            dependencies.RemoveAll(d => !IsServedByOtherSchema(d, sourceSchema));

            var keyFieldNames = dependencies
                .Where(d => d.Coordinate.Name == mergedType.Name)
                .Select(d => d.Coordinate.MemberName)
                .ToHashSet();

            foreach (var servedField in sourceType.Fields)
            {
                if (servedField is { IsExternal: false, IsInternal: false, IsOverridden: false }
                    && !keyFieldNames.Contains(servedField.Name)
                    && mergedType.Fields.TryGetField(servedField.Name, out var mergedField))
                {
                    Check(mergedField, dependencies, lookupField.Coordinate, sourceSchema);
                }
            }
        }

        private bool IsServedByOtherSchema(
            IOutputFieldDefinition field,
            MutableSchemaDefinition sourceSchema)
        {
            foreach (var other in context.SchemaDefinitions)
            {
                if (!ReferenceEquals(other, sourceSchema)
                    && other.Types.TryGetType<MutableComplexTypeDefinition>(
                        field.Coordinate.Name,
                        out var otherType)
                    && otherType.Fields.TryGetField(field.Name, out var otherField)
                    && otherField is { IsExternal: false, IsInternal: false, IsOverridden: false })
                {
                    return true;
                }
            }

            return false;
        }

        private List<IOutputFieldDefinition> SelectFields(
            string map,
            MutableInputFieldDefinition argument,
            MutableComplexTypeDefinition mergedType)
        {
            var inputTypeName = argument.Type.AsTypeDefinition().Name;

            if (!schema.Types.TryGetType(inputTypeName, out var inputTypeDefinition))
            {
                return [];
            }

            var inputType = argument.Type.ToTypeNode().RewriteToType(inputTypeDefinition);

            var selection = new FieldSelectionMapParser(map).Parse();
            var selectedFields = new HashSet<IOutputFieldDefinition>();

            foreach (var variant in ExpandChoices(selection))
            {
                _validator.Validate(variant, inputType, mergedType, out var variantFields);
                selectedFields.UnionWith(variantFields);
            }

            return [.. selectedFields.OrderBy(f => f.Coordinate.ToString(), StringComparer.Ordinal)];
        }

        private static IEnumerable<IValueSelectionNode> ExpandChoices(IValueSelectionNode node)
        {
            switch (node)
            {
                case ChoiceValueSelectionNode choice:
                    foreach (var branch in choice.Branches)
                    {
                        foreach (var variant in ExpandChoices(branch))
                        {
                            yield return variant;
                        }
                    }

                    break;

                case ObjectValueSelectionNode objectSelection:
                    foreach (var variant in ExpandObject(objectSelection))
                    {
                        yield return variant;
                    }

                    break;

                case ListValueSelectionNode listSelection:
                    foreach (var variant in ExpandList(listSelection))
                    {
                        yield return variant;
                    }

                    break;

                case PathObjectValueSelectionNode pathObject:
                    foreach (var variant in ExpandObject(pathObject.ObjectValueSelection))
                    {
                        yield return new PathObjectValueSelectionNode(
                            pathObject.Location,
                            pathObject.Path,
                            variant);
                    }

                    break;

                case PathListValueSelectionNode pathList:
                    foreach (var variant in ExpandList(pathList.ListValueSelection))
                    {
                        yield return new PathListValueSelectionNode(
                            pathList.Location,
                            pathList.Path,
                            variant);
                    }

                    break;

                default:
                    yield return node;
                    break;
            }
        }

        private static IEnumerable<ObjectValueSelectionNode> ExpandObject(
            ObjectValueSelectionNode node)
        {
            IEnumerable<ImmutableArray<ObjectFieldSelectionNode>> combinations = [[]];

            foreach (var field in node.Fields)
            {
                IValueSelectionNode?[] variants = field.ValueSelection is { } valueSelection
                    ? [.. ExpandChoices(valueSelection)]
                    : [null];
                var current = combinations;

                combinations = current.SelectMany(
                    prefix => variants.Select(
                        variant => prefix.Add(
                            new ObjectFieldSelectionNode(
                                field.Location,
                                field.Name,
                                field.Arguments,
                                variant))));
            }

            foreach (var fields in combinations)
            {
                yield return new ObjectValueSelectionNode(node.Location, fields);
            }
        }

        private static IEnumerable<ListValueSelectionNode> ExpandList(ListValueSelectionNode node)
        {
            foreach (var variant in ExpandChoices(node.ElementSelection))
            {
                yield return new ListValueSelectionNode(node.Location, variant);
            }
        }

        private void Check(
            IOutputFieldDefinition requiring,
            List<IOutputFieldDefinition> dependencies,
            SchemaCoordinate via,
            MutableSchemaDefinition sourceSchema)
        {
            var requirement = GetRequirement(requiring);

            foreach (var dependency in dependencies)
            {
                var uncovered = requirement.GetUncovered(GetRequirement(dependency));

                if (uncovered.IsEmpty
                    || !_reported.Add((requiring.Coordinate.ToString(), dependency.Coordinate.ToString())))
                {
                    continue;
                }

                context.Log.Write(
                    AuthorizationTransitiveRequirementsMissing(
                        requiring.Coordinate,
                        dependency.Coordinate,
                        via,
                        sourceSchema,
                        uncovered));
            }
        }

        private static MergedAuthorization GetRequirement(IOutputFieldDefinition field)
        {
            return field.Directives.FirstOrDefault(FusionAuthorization) is { } directive
                ? AuthorizationGroups.FromFusionDirective(directive)
                : MergedAuthorization.Empty;
        }
    }
}
