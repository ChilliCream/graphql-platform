using HotChocolate.Fusion.ApolloFederation;
using HotChocolate.Fusion.Events;
using HotChocolate.Fusion.Events.Contracts;
using HotChocolate.Fusion.Extensions;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using static HotChocolate.Fusion.Logging.LogEntryHelper;

namespace HotChocolate.Fusion.SourceSchemaValidationRules;

/// <summary>
/// <para>
/// The <c>@provides</c> directive indicates that an object type field will supply additional fields
/// belonging to the return type in this execution-specific path. Any field listed in the
/// <c>@provides(fields: ...)</c> argument must therefore be <i>external</i> in the local schema,
/// meaning that the local schema itself does <b>not</b> provide it.
/// </para>
/// <para>
/// This rule disallows selecting non-external fields in a <c>@provides</c> selection set. If a
/// field is already provided by the same schema in all execution paths, there is no need to
/// <c>@provide</c>.
/// </para>
/// </summary>
/// <seealso href="https://graphql.github.io/composite-schemas-spec/draft/#sec-Provides-Fields-Missing-External">
/// Specification
/// </seealso>
internal sealed class ProvidesFieldsMissingExternalRule : IEventHandler<OutputFieldEvent>
{
    public void Handle(OutputFieldEvent @event, CompositionContext context)
    {
        var (field, _, schema) = @event;

        if (field.ProvidesInfo is not { } providesInfo)
        {
            return;
        }

        if (schema.Features.Get<ConnectorKindMetadata>()?.Kind == "ApolloFederation"
            && providesInfo.SelectionSet is { } selectionSet)
        {
            ValidateApolloFederationSelections(
                selectionSet,
                field.Type.AsTypeDefinition(),
                hasExternalInParents: false,
                providesInfo.Directive,
                field,
                schema,
                context);
            return;
        }

        foreach (var providedField in providesInfo.Fields)
        {
            if (!providedField.IsExternal)
            {
                context.Log.Write(
                    ProvidesFieldsMissingExternal(
                        providedField,
                        providesInfo.Directive,
                        field,
                        schema));
            }
        }
    }

    private static void ValidateApolloFederationSelections(
        SelectionSetNode selectionSet,
        ITypeDefinition type,
        bool hasExternalInParents,
        Directive providesDirective,
        MutableOutputFieldDefinition providingField,
        MutableSchemaDefinition schema,
        CompositionContext context)
    {
        foreach (var selection in selectionSet.Selections)
        {
            switch (selection)
            {
                case FieldNode fieldNode
                    when type is MutableComplexTypeDefinition complexType
                        && complexType.Fields.TryGetField(
                            fieldNode.Name.Value,
                            out var providedField):
                    if (fieldNode.SelectionSet is null)
                    {
                        if (!hasExternalInParents && !providedField.IsExternal)
                        {
                            context.Log.Write(
                                ProvidesFieldsMissingExternal(
                                    providedField,
                                    providesDirective,
                                    providingField,
                                    schema));
                        }

                        break;
                    }

                    var hasExternalInPath = hasExternalInParents || providedField.IsExternal;

                    if (!hasExternalInPath
                        && complexType is MutableInterfaceTypeDefinition interfaceType)
                    {
                        foreach (var possibleType in schema.GetPossibleTypes(interfaceType))
                        {
                            if (possibleType.Fields.TryGetField(
                                    providedField.Name,
                                    out var possibleField)
                                && possibleField.IsExternal)
                            {
                                hasExternalInPath = true;
                                break;
                            }
                        }
                    }

                    ValidateApolloFederationSelections(
                        fieldNode.SelectionSet,
                        providedField.Type.AsTypeDefinition(),
                        hasExternalInPath,
                        providesDirective,
                        providingField,
                        schema,
                        context);
                    break;

                case InlineFragmentNode inlineFragment:
                    var fragmentType = type;

                    if (inlineFragment.TypeCondition is { } typeCondition
                        && schema.Types.TryGetType(
                            typeCondition.Name.Value,
                            out var resolvedType))
                    {
                        fragmentType = resolvedType;
                    }

                    ValidateApolloFederationSelections(
                        inlineFragment.SelectionSet,
                        fragmentType,
                        hasExternalInParents,
                        providesDirective,
                        providingField,
                        schema,
                        context);
                    break;
            }
        }
    }
}
