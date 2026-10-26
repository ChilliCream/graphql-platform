using System.Collections.Immutable;
using HotChocolate.Fusion.Extensions;
using HotChocolate.Fusion.Info;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion;

internal sealed partial class SourceSchemaMerger
{
    /// <summary>
    /// Merges the definitions of a client directive into a definition that every source schema
    /// accepts, and adds it to <paramref name="mergedSchema"/>. The directive is left out when a
    /// source schema does not define it or when the definitions share no forwarded executable
    /// location.
    /// </summary>
    private void MergeExecutableDirectiveDefinition(
        ImmutableArray<DirectiveDefinitionInfo> definitionGroup,
        MutableSchemaDefinition mergedSchema)
    {
        if (definitionGroup.Length != _schemas.Count
            || !definitionGroup.All(i => i.DirectiveDefinition.IsComposableExecutableDirective))
        {
            return;
        }

        var locations = DirectiveDefinitionExtensions.ForwardedExecutableLocations;
        var isRepeatable = true;
        string? description = null;

        foreach (var (definition, _) in definitionGroup)
        {
            locations &= definition.Locations;
            isRepeatable &= definition.IsRepeatable;
            description ??= definition.Description;
        }

        if (locations == 0)
        {
            return;
        }

        var mergedDirective = new MutableDirectiveDefinition(definitionGroup[0].DirectiveDefinition.Name)
        {
            Description = description,
            IsRepeatable = isRepeatable,
            Locations = locations
        };

        var argumentNames = definitionGroup
            .SelectMany(i => i.DirectiveDefinition.Arguments.AsEnumerable(), (_, a) => a.Name)
            .Distinct(StringComparer.Ordinal);

        foreach (var argumentName in argumentNames)
        {
            var arguments = new List<MutableInputFieldDefinition>(definitionGroup.Length);

            foreach (var (definition, _) in definitionGroup)
            {
                if (definition.Arguments.TryGetField(argumentName, out var argument))
                {
                    arguments.Add(argument);
                }
            }

            var isShared =
                arguments.Count == definitionGroup.Length
                && arguments.All(a => !a.HasInaccessibleDirective() && !IsInaccessibleType(a.Type));

            if (!isShared)
            {
                // An argument that is not shared leaves the directive out when a source schema requires it.
                if (arguments.Any(a => a.Type is NonNullType && a.DefaultValue is null))
                {
                    return;
                }

                continue;
            }

            var mergedArgument = MergeExecutableDirectiveArgument(arguments, mergedSchema);

            if (mergedArgument is null)
            {
                return;
            }

            mergedArgument.DeclaringMember = mergedDirective;
            mergedDirective.Arguments.Add(mergedArgument);
        }

        mergedSchema.DirectiveDefinitions.Add(mergedDirective);
    }

    /// <summary>
    /// Merges the definitions of a client directive argument that every source schema declares,
    /// or returns <see langword="null"/> when their types cannot be merged. The merged argument
    /// has a default value only when every source schema declares the same one.
    /// </summary>
    private static MutableInputFieldDefinition? MergeExecutableDirectiveArgument(
        List<MutableInputFieldDefinition> arguments,
        MutableSchemaDefinition mergedSchema)
    {
        var first = arguments[0];

        if (!arguments.All(a => TypeMergeHelper.SameTypeShape(first.Type, a.Type))
            || !mergedSchema.Types.TryGetType(first.Type.NamedType().Name, out var namedType)
            || namedType is not IInputTypeDefinition)
        {
            return null;
        }

        IType type = first.Type;
        string? description = null;
        string? deprecationReason = null;
        var hasSharedDefaultValue = true;

        foreach (var argument in arguments)
        {
            type = MostRestrictiveType(type, argument.Type);
            description ??= argument.Description;
            deprecationReason ??= argument.DeprecationReason;
            hasSharedDefaultValue &= SyntaxComparer.BySyntax.Equals(argument.DefaultValue, first.DefaultValue);
        }

        return new MutableInputFieldDefinition(first.Name, type.ReplaceNamedType(_ => namedType).ExpectInputType())
        {
            DefaultValue = hasSharedDefaultValue ? first.DefaultValue : null,
            Description = description,
            DeprecationReason = deprecationReason
        };
    }

    private bool IsInaccessibleType(IType type)
    {
        var typeName = type.NamedType().Name;

        foreach (var schema in _schemas)
        {
            if (schema.Types.TryGetType(typeName, out var sourceType)
                && sourceType is IDirectivesProvider directivesProvider
                && directivesProvider.HasInaccessibleDirective())
            {
                return true;
            }
        }

        return false;
    }
}
