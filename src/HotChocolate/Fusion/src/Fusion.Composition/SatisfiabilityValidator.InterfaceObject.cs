using HotChocolate.Fusion.Extensions;
using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Satisfiability;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using static HotChocolate.Fusion.Properties.CompositionResources;
using static HotChocolate.Language.Utf8GraphQLParser.Syntax;
using ArgumentNames = HotChocolate.Fusion.WellKnownArgumentNames;
using DirectiveNames = HotChocolate.Fusion.WellKnownDirectiveNames;

namespace HotChocolate.Fusion;

internal sealed partial class SatisfiabilityValidator
{
    /// <summary>
    /// Validates the satisfiability obligations that arise from <c>@interfaceObject</c> stand-ins.
    /// A value of an interface produced through a stand-in schema is opaque: that schema holds no
    /// authoritative concrete type for it. Recovering identity or fetching non-local data for such
    /// a value requires a covering interface lookup, and every projected default field must be
    /// reachable through one of the stand-in's own lookups.
    /// </summary>
    private void ValidateInterfaceObjectBindings()
    {
        foreach (var interfaceType in _schema.Types.OfType<MutableInterfaceTypeDefinition>())
        {
            var standInSchemaNames =
                MutableSchemaDefinitionExtensions.GetInterfaceObjectSchemaNames(interfaceType).ToArray();

            if (standInSchemaNames.Length == 0)
            {
                continue;
            }

            var keyFieldNames = GetInterfaceKeyFieldNames(interfaceType);

            foreach (var standInSchemaName in standInSchemaNames)
            {
                if (AllowsNonResolvableInterfaceObject(standInSchemaName))
                {
                    continue;
                }

                ValidateProjectedFieldsPlannable(interfaceType, standInSchemaName, keyFieldNames);

                var opaquePosition = FindClientAccessibleOpaquePosition(interfaceType, standInSchemaName);

                if (opaquePosition is { } position
                    && !HasCoveringLookup(interfaceType, keyFieldNames))
                {
                    ReportNoCoveringLookup(interfaceType, standInSchemaName, position);
                }
            }
        }
    }

    private bool AllowsNonResolvableInterfaceObject(string schemaName)
        => _apolloFederationCompatibility.AllowNonResolvableInterfaceObjects
            && _apolloFederationSchemaNames.Contains(schemaName);

    /// <summary>
    /// Every non-key field a stand-in contributes is projected onto the implementing types as a
    /// default. The stand-in schema must declare a lookup through which the executor can fetch it;
    /// without one, no plan can ever reach the field.
    /// </summary>
    private void ValidateProjectedFieldsPlannable(
        MutableInterfaceTypeDefinition interfaceType,
        string standInSchemaName,
        HashSet<string> keyFieldNames)
    {
        var contributesProjectedField = false;

        foreach (var field in interfaceType.Fields)
        {
            if (!keyFieldNames.Contains(field.Name)
                && FieldHasSource(field, standInSchemaName))
            {
                contributesProjectedField = true;
                break;
            }
        }

        if (!contributesProjectedField)
        {
            return;
        }

        if (!HasLookupInSchema(interfaceType, standInSchemaName))
        {
            ReportError(
                string.Format(
                    SatisfiabilityValidator_InterfaceObjectNoLookup,
                    standInSchemaName,
                    interfaceType.Name));
        }
    }

    /// <summary>
    /// A covering interface lookup is a lookup, in a single source schema, that returns the
    /// interface itself, whose inputs are resolvable from the stand-in's key fields, and whose
    /// schema-local possible-type set is a superset of the composite possible-type set.
    /// </summary>
    private bool HasCoveringLookup(
        MutableInterfaceTypeDefinition interfaceType,
        HashSet<string> keyFieldNames)
    {
        var compositePossibleTypes = GetCompositePossibleTypeNames(interfaceType);

        foreach (var candidateSchemaName in GetInterfaceDefiningSchemaNames(interfaceType))
        {
            var localPossibleTypes = interfaceType
                .GetPossibleTypes(candidateSchemaName, _schema)
                .Select(t => t.Name)
                .ToHashSet(StringComparer.Ordinal);

            if (!localPossibleTypes.IsSupersetOf(compositePossibleTypes))
            {
                continue;
            }

            foreach (var lookup in GetLookupsInSchema(interfaceType, candidateSchemaName))
            {
                if (LookupReturnsInterface(lookup, interfaceType)
                    && GetLookupKeyFieldNames(lookup).IsSubsetOf(keyFieldNames))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private void ReportNoCoveringLookup(
        MutableInterfaceTypeDefinition interfaceType,
        string standInSchemaName,
        OpaquePosition opaquePosition)
    {
        var compositePossibleTypes = GetCompositePossibleTypeNames(interfaceType);
        var coveredTypes = new HashSet<string>(StringComparer.Ordinal);

        foreach (var candidateSchemaName in GetInterfaceDefiningSchemaNames(interfaceType))
        {
            var hasInterfaceLookup =
                GetLookupsInSchema(interfaceType, candidateSchemaName)
                    .Any(l => LookupReturnsInterface(l, interfaceType));

            if (!hasInterfaceLookup)
            {
                continue;
            }

            foreach (var possibleType in interfaceType.GetPossibleTypes(candidateSchemaName, _schema))
            {
                coveredTypes.Add(possibleType.Name);
            }
        }

        var uncoveredTypes = compositePossibleTypes
            .Where(t => !coveredTypes.Contains(t))
            .OrderBy(t => t, StringComparer.Ordinal)
            .ToArray();

        var introducingSchemas = uncoveredTypes
            .SelectMany(GetTypeDefiningSchemaNames)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(s => s, StringComparer.Ordinal)
            .ToArray();

        ReportError(
            string.Format(
                SatisfiabilityValidator_InterfaceObjectNoCoveringLookup,
                $"{opaquePosition.ParentTypeName}.{opaquePosition.FieldName}",
                interfaceType.Name,
                standInSchemaName,
                string.Join(", ", uncoveredTypes.Select(t => $"'{t}'")),
                string.Join(", ", introducingSchemas.Select(s => $"'{s}'"))));
    }

    private OpaquePosition? FindClientAccessibleOpaquePosition(
        MutableInterfaceTypeDefinition interfaceType,
        string standInSchemaName)
    {
        foreach (var type in _schema.Types.OfType<MutableComplexTypeDefinition>())
        {
            foreach (var field in type.Fields)
            {
                if (field.HasFusionInaccessibleDirective()
                    || field.Type.NamedType() is not ITypeDefinition namedType
                    || namedType.Name != interfaceType.Name)
                {
                    continue;
                }

                // The field returns the interface and is resolved by a stand-in schema of it, so
                // the value it produces is opaque.
                if (FieldHasSource(field, standInSchemaName))
                {
                    return new OpaquePosition(type.Name, field.Name);
                }
            }
        }

        return null;
    }

    private static bool FieldHasSource(MutableOutputFieldDefinition field, string schemaName)
    {
        foreach (var directive in field.Directives.AsEnumerable())
        {
            if (directive.Name == DirectiveNames.FusionField
                && directive.Arguments.TryGetValue(ArgumentNames.Schema, out var schema)
                && schema is EnumValueNode { Value: var schemaValue }
                && schemaValue == schemaName)
            {
                return true;
            }
        }

        return false;
    }

    private HashSet<string> GetCompositePossibleTypeNames(MutableInterfaceTypeDefinition interfaceType)
        => _schema.GetPossibleTypes(interfaceType)
            .Select(t => t.Name)
            .ToHashSet(StringComparer.Ordinal);

    private static IEnumerable<string> GetInterfaceDefiningSchemaNames(
        MutableInterfaceTypeDefinition interfaceType)
    {
        var standInSchemaNames = MutableSchemaDefinitionExtensions
            .GetInterfaceObjectSchemaNames(interfaceType)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var directive in interfaceType.Directives.AsEnumerable())
        {
            if (directive.Name == DirectiveNames.FusionType
                && directive.Arguments[ArgumentNames.Schema] is EnumValueNode { Value: var schemaName }
                && !standInSchemaNames.Contains(schemaName))
            {
                yield return schemaName;
            }
        }
    }

    private IEnumerable<string> GetTypeDefiningSchemaNames(string typeName)
    {
        if (!_schema.Types.TryGetType(typeName, out var type)
            || type is not MutableComplexTypeDefinition complexType)
        {
            yield break;
        }

        foreach (var directive in complexType.Directives.AsEnumerable())
        {
            if (directive.Name == DirectiveNames.FusionType
                && directive.Arguments[ArgumentNames.Schema] is EnumValueNode { Value: var schemaName })
            {
                yield return schemaName;
            }
        }
    }

    private static HashSet<string> GetInterfaceKeyFieldNames(MutableInterfaceTypeDefinition interfaceType)
    {
        var keyFieldNames = new HashSet<string>(StringComparer.Ordinal);

        foreach (var directive in interfaceType.Directives.AsEnumerable())
        {
            if (directive.Name != DirectiveNames.FusionLookup)
            {
                continue;
            }

            foreach (var fieldName in GetLookupKeyFieldNames(directive))
            {
                keyFieldNames.Add(fieldName);
            }
        }

        return keyFieldNames;
    }

    private static IEnumerable<IDirective> GetLookupsInSchema(
        MutableInterfaceTypeDefinition interfaceType,
        string schemaName)
    {
        foreach (var directive in interfaceType.Directives.AsEnumerable())
        {
            if (directive.Name == DirectiveNames.FusionLookup
                && directive.Arguments[ArgumentNames.Schema] is EnumValueNode { Value: var lookupSchema }
                && lookupSchema == schemaName)
            {
                yield return directive;
            }
        }
    }

    private static bool HasLookupInSchema(MutableInterfaceTypeDefinition interfaceType, string schemaName)
        => GetLookupsInSchema(interfaceType, schemaName).Any();

    private static bool LookupReturnsInterface(IDirective lookup, MutableInterfaceTypeDefinition interfaceType)
    {
        var fieldArgument = (string)lookup.Arguments[ArgumentNames.Field].Value!;
        var fieldDefinition = ParseFieldDefinition(fieldArgument);

        return fieldDefinition.Type.NamedType().Name.Value == interfaceType.Name;
    }

    private static HashSet<string> GetLookupKeyFieldNames(IDirective lookup)
    {
        var keyFieldNames = new HashSet<string>(StringComparer.Ordinal);
        var key = (string)lookup.Arguments[ArgumentNames.Key].Value!;
        var selectionSet = ParseSelectionSet($"{{ {key} }}");

        CollectTopLevelFieldNames(selectionSet, keyFieldNames);

        return keyFieldNames;
    }

    private static void CollectTopLevelFieldNames(SelectionSetNode selectionSet, HashSet<string> fieldNames)
    {
        foreach (var selection in selectionSet.Selections)
        {
            switch (selection)
            {
                case FieldNode field:
                    fieldNames.Add(field.Name.Value);
                    break;

                case InlineFragmentNode inlineFragment:
                    CollectTopLevelFieldNames(inlineFragment.SelectionSet, fieldNames);
                    break;
            }
        }
    }

    private void ReportError(string message)
    {
        var error = new SatisfiabilityError(message);

        _log.Write(
            LogEntryBuilder.New()
                .SetMessage(error.ToString())
                .SetCode(LogEntryCodes.UnsatisfiableQueryPath)
                .SetSeverity(LogSeverity.Error)
                .SetExtension("error", error)
                .Build());
    }

    private readonly record struct OpaquePosition(string ParentTypeName, string FieldName);
}
