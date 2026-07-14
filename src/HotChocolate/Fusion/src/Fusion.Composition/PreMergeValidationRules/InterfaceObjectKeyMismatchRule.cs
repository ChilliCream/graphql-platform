using HotChocolate.Fusion.Events;
using HotChocolate.Fusion.Events.Contracts;
using HotChocolate.Language;
using HotChocolate.Types.Mutable;
using static HotChocolate.Fusion.Logging.LogEntryHelper;
using ArgumentNames = HotChocolate.Fusion.WellKnownArgumentNames;

namespace HotChocolate.Fusion.PreMergeValidationRules;

/// <summary>
/// An interface that has a stand-in must declare at least one key with <c>@key</c>, and the stand-in
/// must key on one of the keys of the interface. Each <c>@key</c> on the stand-in must select the
/// same fields as a <c>@key</c> declared on the interface by at least one interface-defining schema.
/// The comparison is structural; the order of the fields and their formatting do not matter.
/// </summary>
/// <seealso href="https://graphql.github.io/composite-schemas-spec/draft/#sec-Interface-Object-Key-Mismatch">
/// Specification
/// </seealso>
internal sealed class InterfaceObjectKeyMismatchRule : IEventHandler<TypeGroupEvent>
{
    public void Handle(TypeGroupEvent @event, CompositionContext context)
    {
        var (_, typeGroup) = @event;

        var standIns = typeGroup
            .Where(i => i.Type is MutableObjectTypeDefinition o
                && o.Directives.ContainsName(WellKnownDirectiveNames.InterfaceObject))
            .ToArray();

        if (standIns.Length == 0)
        {
            return;
        }

        // Collect the structural key selections declared on the interface-defining schemas. When the
        // interface declares no key, INTERFACE_OBJECT_KEY_MISMATCH also fires, because the stand-in
        // can then never key on the interface.
        var interfaceKeys = new HashSet<string>(StringComparer.Ordinal);

        foreach (var typeInfo in typeGroup)
        {
            if (typeInfo.Type is not MutableInterfaceTypeDefinition interfaceType)
            {
                continue;
            }

            foreach (var keyDirective in interfaceType.Directives.AsEnumerable())
            {
                if (keyDirective.Name != WellKnownDirectiveNames.Key
                    || TryNormalizeKey(keyDirective) is not { } key)
                {
                    continue;
                }

                interfaceKeys.Add(key);
            }
        }

        foreach (var (type, schema) in standIns)
        {
            var standIn = (MutableObjectTypeDefinition)type;

            foreach (var keyDirective in standIn.Directives.AsEnumerable())
            {
                if (keyDirective.Name != WellKnownDirectiveNames.Key
                    || TryNormalizeKey(keyDirective) is not { } key)
                {
                    continue;
                }

                if (!interfaceKeys.Contains(key))
                {
                    context.Log.Write(InterfaceObjectKeyMismatch(standIn, key, schema));
                }
            }
        }
    }

    private static string? TryNormalizeKey(Directive keyDirective)
    {
        if (!keyDirective.Arguments.TryGetValue(ArgumentNames.Fields, out var value)
            || value is not StringValueNode fields)
        {
            return null;
        }

        try
        {
            return Canonicalize(Utf8GraphQLParser.Syntax.ParseSelectionSet($"{{ {fields.Value} }}"));
        }
        catch (SyntaxException)
        {
            return null;
        }
    }

    private static string Canonicalize(SelectionSetNode selectionSet)
    {
        var parts = selectionSet.Selections
            .OfType<FieldNode>()
            .Select(f => f.SelectionSet is null
                ? f.Name.Value
                : $"{f.Name.Value}{{{Canonicalize(f.SelectionSet)}}}")
            .OrderBy(p => p, StringComparer.Ordinal);

        return string.Join(" ", parts);
    }
}
