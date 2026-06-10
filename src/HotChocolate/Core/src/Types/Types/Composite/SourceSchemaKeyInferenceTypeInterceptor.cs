using HotChocolate.Configuration;
using HotChocolate.Fusion.Language;
using HotChocolate.Fusion.Rewriters;
using HotChocolate.Internal;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Configurations;

namespace HotChocolate.Types.Composite;

/// <summary>
/// Infers @key directives from the arguments of @lookup fields and applies them to the
/// types that the lookups resolve so that the published source schema describes the
/// entity keys.
/// </summary>
internal sealed class SourceSchemaKeyInferenceTypeInterceptor : TypeInterceptor
{
    private const string IsFieldArgumentName = "field";

    private readonly List<LookupInfo> _lookups = [];
    private readonly Dictionary<string, TypeConfiguration> _typesByName = new(StringComparer.Ordinal);
    private readonly Dictionary<string, List<TypeConfiguration>> _implementersByInterface =
        new(StringComparer.Ordinal);
    private readonly Dictionary<string, List<TypeConfiguration>> _membersByUnion =
        new(StringComparer.Ordinal);
    private ITypeCompletionContext? _completionContext;
    private TypeReference _entityKeyRef = null!;

    public override bool IsEnabled(IDescriptorContext context)
        => context.Options.InferKeysFromLookups;

    public override void OnBeforeRegisterDependencies(
        ITypeDiscoveryContext discoveryContext,
        TypeSystemConfiguration configuration)
    {
        // If a lookup is present we make sure that the @key directive type is registered,
        // so that the inferred directive can be resolved even when no @key was declared manually.
        if (configuration is not (ObjectTypeConfiguration or InterfaceTypeConfiguration))
        {
            return;
        }

        if (!HasInferableLookupField((TypeConfiguration)configuration))
        {
            return;
        }

        _entityKeyRef ??= discoveryContext.TypeInspector.GetTypeRef(typeof(EntityKey));
        discoveryContext.Dependencies.Add(new TypeDependency(_entityKeyRef));
    }

    public override void OnAfterCompleteName(
        ITypeCompletionContext completionContext,
        TypeSystemConfiguration configuration)
    {
        // We capture a completion context so that we can resolve type references once all
        // type names are completed.
        _completionContext ??= completionContext;

        switch (configuration)
        {
            case ObjectTypeConfiguration objectType:
                // We only index the base type, not type extensions. Extensions are merged into the
                // base before keys are applied, so the indexed base carries the merged directives
                // that the deduplication relies on. Lookups declared on an extension are still
                // collected below.
                if (!objectType.IsExtension)
                {
                    _typesByName[objectType.Name] = objectType;
                }
                CollectLookups(objectType.Fields);
                break;

            case InterfaceTypeConfiguration interfaceType:
                _typesByName[interfaceType.Name] = interfaceType;
                CollectLookups(interfaceType.Fields);
                break;

            case UnionTypeConfiguration unionType:
                _typesByName[unionType.Name] = unionType;
                break;
        }
    }

    public override void OnBeforeCompleteTypes()
    {
        if (_lookups.Count == 0 || _completionContext is null)
        {
            return;
        }

        var context = _completionContext;

        BuildImplementerIndex(context);

        foreach (var lookup in _lookups)
        {
            if (!context.TryGetType<IType>(lookup.ReturnTypeRef, out var returnType))
            {
                continue;
            }

            var namedType = returnType.NamedType();

            // We only infer keys for nullable, non-list lookups, mirroring the composer.
            if (returnType.IsNonNullType() || returnType.IsListType())
            {
                continue;
            }

            var targets = ResolveTargets(namedType);

            if (targets.Count == 0)
            {
                continue;
            }

            foreach (var keyFields in BuildKeySelectionSets(lookup))
            {
                foreach (var target in targets)
                {
                    ApplyKey(context, target, keyFields);
                }
            }
        }
    }

    private void CollectLookups<TField>(IEnumerable<TField> fields)
        where TField : OutputFieldConfiguration
    {
        foreach (var field in fields)
        {
            if (field.Type is null || !HasDirective(field.GetDirectives(), DirectiveNames.Lookup.Name))
            {
                continue;
            }

            // We exclude lookups without arguments, mirroring the composer which cannot derive any
            // key fields from an argument-less lookup.
            if (!field.HasArguments)
            {
                continue;
            }

            _lookups.Add(new LookupInfo(field, field.Type));
        }
    }

    private void BuildImplementerIndex(ITypeCompletionContext context)
    {
        foreach (var (_, configuration) in _typesByName)
        {
            switch (configuration)
            {
                // We only index object types as interface implementers, mirroring the composer's
                // inference pass which applies inferred keys to the possible (object) types of an
                // interface and the interface itself. Sub-interfaces are intentionally excluded.
                case ObjectTypeConfiguration objectType:
                    foreach (var interfaceRef in objectType.GetInterfaces())
                    {
                        if (context.TryGetType<IType>(interfaceRef, out var interfaceType)
                            && interfaceType.NamedType() is IInterfaceTypeDefinition i)
                        {
                            AddToIndex(_implementersByInterface, i.Name, objectType);
                        }
                    }
                    break;

                case UnionTypeConfiguration unionType:
                    foreach (var memberRef in unionType.Types)
                    {
                        if (context.TryGetType<IType>(memberRef, out var memberType)
                            && memberType.NamedType() is IObjectTypeDefinition o
                            && _typesByName.TryGetValue(o.Name, out var memberConfig))
                        {
                            AddToIndex(_membersByUnion, unionType.Name, memberConfig);
                        }
                    }
                    break;
            }
        }
    }

    private List<TypeConfiguration> ResolveTargets(ITypeDefinition namedType)
    {
        var targets = new List<TypeConfiguration>();

        switch (namedType)
        {
            case IObjectTypeDefinition objectType:
                if (_typesByName.TryGetValue(objectType.Name, out var objectConfig))
                {
                    targets.Add(objectConfig);
                }
                break;

            case IInterfaceTypeDefinition interfaceType:
                if (_typesByName.TryGetValue(interfaceType.Name, out var interfaceConfig))
                {
                    targets.Add(interfaceConfig);
                }

                if (_implementersByInterface.TryGetValue(interfaceType.Name, out var implementers))
                {
                    targets.AddRange(implementers);
                }
                break;

            case IUnionTypeDefinition unionType:
                // The @key directive is only valid on OBJECT and INTERFACE, so we apply it to the
                // members of the union and not to the union itself.
                if (_membersByUnion.TryGetValue(unionType.Name, out var members))
                {
                    targets.AddRange(members);
                }
                break;
        }

        return targets;
    }

    private static IEnumerable<SelectionSetNode> BuildKeySelectionSets(LookupInfo lookup)
    {
        // We convert the @is value selections into @key selection sets purely syntactically so
        // that the conversion does not depend on the completed type graph. This lets nested @is
        // paths (for example "address.id") be turned into nested selection sets ("address { id }")
        // even though the referenced child type is not yet completed at this phase. Malformed
        // value selections are filtered out earlier while parsing, so genuine bugs surface here
        // instead of being silently dropped.
        foreach (var group in GetValueSelectionGroups(lookup.Field))
        {
            yield return ValueSelectionToSelectionSetRewriter.Rewrite(group);
        }
    }

    private static List<List<IValueSelectionNode>> GetValueSelectionGroups(OutputFieldConfiguration field)
    {
        var arrays = new List<IValueSelectionNode[]>();

        foreach (var argument in field.GetArguments())
        {
            var selectionMap = GetIsFieldSelectionMap(argument) ?? argument.Name;

            IValueSelectionNode parsed;

            try
            {
                parsed = FieldSelectionMapParser.Parse(selectionMap);
            }
            catch (FieldSelectionMapSyntaxException)
            {
                return [];
            }

            if (parsed is ChoiceValueSelectionNode choice)
            {
                arrays.Add([.. choice.Branches]);
            }
            else
            {
                arrays.Add([parsed]);
            }
        }

        return GetAllCombinations(arrays);
    }

    private static string? GetIsFieldSelectionMap(ArgumentConfiguration argument)
    {
        foreach (var directive in argument.GetDirectives())
        {
            switch (directive.Value)
            {
                case Is @is:
                    return @is.Field.ToString(false);

                case DirectiveNode { Name.Value: DirectiveNames.Is.Name } node:
                    var fieldArgument = node.Arguments
                        .FirstOrDefault(a => a.Name.Value == IsFieldArgumentName);

                    if (fieldArgument?.Value is StringValueNode stringValue)
                    {
                        return stringValue.Value;
                    }
                    break;
            }
        }

        return null;
    }

    private static void ApplyKey(
        ITypeCompletionContext context,
        TypeConfiguration target,
        SelectionSetNode keyFields)
    {
        // We deduplicate keys order-insensitively and structurally (so "id sku" equals "sku id"
        // and "address { id }" is compared by shape). This also suppresses an inferred key when an
        // equivalent key was declared manually, which keeps the interceptor idempotent against
        // manually authored keys.
        var normalizedKey = NormalizeSelectionSet(keyFields);

        foreach (var directive in target.GetDirectives())
        {
            if (TryGetKeySelectionSet(directive.Value, out var existing)
                && string.Equals(NormalizeSelectionSet(existing), normalizedKey, StringComparison.Ordinal))
            {
                return;
            }
        }

        target.AddDirective(new EntityKey(keyFields), context.TypeInspector);
    }

    private static bool TryGetKeySelectionSet(object value, out SelectionSetNode selectionSet)
    {
        switch (value)
        {
            case EntityKey entityKey:
                selectionSet = entityKey.Fields;
                return true;

            case DirectiveNode { Name.Value: DirectiveNames.Key.Name } node:
                var fieldsArgument = node.Arguments
                    .FirstOrDefault(a => a.Name.Value == DirectiveNames.Key.Arguments.Fields);

                if (fieldsArgument?.Value is StringValueNode stringValue)
                {
                    try
                    {
                        selectionSet = FieldSelectionSetType.ParseSelectionSet(stringValue.Value);
                        return true;
                    }
                    catch (SyntaxException)
                    {
                        // ignore invalid manual key syntax for deduplication purposes.
                    }
                }
                break;
        }

        selectionSet = null!;
        return false;
    }

    private static string NormalizeSelectionSet(SelectionSetNode selectionSet)
    {
        var entries = new List<string>();

        foreach (var selection in selectionSet.Selections)
        {
            switch (selection)
            {
                case FieldNode field:
                    var nested = field.SelectionSet is null
                        ? string.Empty
                        : "{" + NormalizeSelectionSet(field.SelectionSet) + "}";
                    entries.Add(field.Name.Value + nested);
                    break;

                case InlineFragmentNode inlineFragment:
                    var typeCondition = inlineFragment.TypeCondition?.Name.Value ?? string.Empty;
                    entries.Add(
                        "..." + typeCondition + "{" + NormalizeSelectionSet(inlineFragment.SelectionSet) + "}");
                    break;
            }
        }

        entries.Sort(StringComparer.Ordinal);

        return string.Join(" ", entries);
    }

    private static bool HasInferableLookupField(TypeConfiguration type)
    {
        switch (type)
        {
            case ObjectTypeConfiguration objectType:
                foreach (var field in objectType.Fields)
                {
                    if (IsInferableLookup(field))
                    {
                        return true;
                    }
                }
                break;

            case InterfaceTypeConfiguration interfaceType:
                foreach (var field in interfaceType.Fields)
                {
                    if (IsInferableLookup(field))
                    {
                        return true;
                    }
                }
                break;
        }

        return false;
    }

    // A lookup can only contribute a key when it is a @lookup field that carries arguments and
    // resolves a nullable, non-list type, mirroring the eligibility applied while keys are built.
    // We restrict the @key type dependency to those fields so that schemas whose lookups never
    // produce a key are not forced to reference the inferred key's scalar.
    private static bool IsInferableLookup(OutputFieldConfiguration field)
    {
        if (field.Type is null
            || !field.HasArguments
            || !HasDirective(field.GetDirectives(), DirectiveNames.Lookup.Name))
        {
            return false;
        }

        return IsNullableNonListReturnType(field.Type);
    }

    private static bool IsNullableNonListReturnType(TypeReference typeReference)
        => typeReference switch
        {
            ExtendedTypeReference extended
                => extended.Type is { IsNullable: true, IsArrayOrList: false },

            SyntaxTypeReference syntax
                => syntax.Type is not (NonNullTypeNode or ListTypeNode),

            // For reference kinds whose nullability cannot be inspected at this phase we keep the
            // dependency so that a key can still be applied later if the resolved type is eligible.
            _ => true
        };

    private static bool HasDirective(
        IReadOnlyList<DirectiveConfiguration> directives,
        string name)
    {
        foreach (var directive in directives)
        {
            switch (directive.Value)
            {
                case Lookup when name == DirectiveNames.Lookup.Name:
                    return true;

                case DirectiveNode node when node.Name.Value == name:
                    return true;
            }
        }

        return false;
    }

    private static void AddToIndex(
        Dictionary<string, List<TypeConfiguration>> index,
        string key,
        TypeConfiguration value)
    {
        if (!index.TryGetValue(key, out var list))
        {
            list = [];
            index[key] = list;
        }

        list.Add(value);
    }

    private static List<List<T>> GetAllCombinations<T>(List<T[]> arrays)
    {
        if (arrays.Count == 0)
        {
            return [[]];
        }

        var result = new List<List<T>> { new() };

        foreach (var array in arrays)
        {
            var temp = new List<List<T>>();

            foreach (var item in array)
            {
                foreach (var combination in result)
                {
                    var newCombination = new List<T>(combination) { item };
                    temp.Add(newCombination);
                }
            }

            result = temp;
        }

        return result;
    }

    private readonly record struct LookupInfo(
        OutputFieldConfiguration Field,
        TypeReference ReturnTypeRef);
}
