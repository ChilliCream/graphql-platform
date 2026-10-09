using System.Collections.Immutable;
using HotChocolate.Fusion.ApolloFederation;
using HotChocolate.Fusion.Events;
using HotChocolate.Fusion.Events.Contracts;
using HotChocolate.Fusion.Extensions;
using HotChocolate.Fusion.Info;
using HotChocolate.Fusion.Options;
using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using static HotChocolate.Fusion.Logging.LogEntryHelper;

namespace HotChocolate.Fusion.PreMergeValidationRules;

/// <summary>
/// <para>
/// This rule ensures that enum types with the same name across different source schemas in a
/// composite schema have identical sets of values. Enums must be consistent across source schemas
/// to avoid conflicts and ambiguities in the composite schema.
/// </para>
/// <para>
/// When an enum is defined with differing values, it can lead to confusion and errors in query
/// execution. For instance, a value valid in one schema might be passed to another where it’s
/// unrecognized, leading to unexpected behavior or failures. This rule prevents such
/// inconsistencies by enforcing that all instances of the same named enum across schemas have an
/// exact match in their values.
/// </para>
/// <para>
/// The configured merge behavior is provided at construction. Under
/// <see cref="EnumValuesMergeBehavior.Union"/>, values of enums used only in output positions
/// merge by union. <see cref="EnumValuesMergeBehavior.Auto"/> applies Union when at least one
/// Apollo Federation connector source schema is part of the composition, otherwise Strict.
/// </para>
/// </summary>
/// <seealso href="https://graphql.github.io/composite-schemas-spec/draft/#sec-Enum-Values-Mismatch">
/// Specification
/// </seealso>
internal sealed class EnumValuesMismatchRule(EnumValuesMergeBehavior enumValuesMergeBehavior)
    : IEventHandler<EnumTypeGroupEvent>
{
    private readonly EnumValuesMergeBehavior _enumValuesMergeBehavior = enumValuesMergeBehavior;

    public void Handle(EnumTypeGroupEvent @event, CompositionContext context)
    {
        var (typeName, enumGroup) = @event;

        if (enumGroup.Length < 2)
        {
            return;
        }

        if (ResolveMergeBehavior(context) is EnumValuesMergeBehavior.Union
            && IsOutputOnlyEnum(typeName, enumGroup))
        {
            return;
        }

        var enumValues = enumGroup
            .SelectMany(e => e.Type.Values.AsEnumerable())
            .Where(v => !v.IsInaccessible)
            .Select(v => v.Name)
            .ToImmutableHashSet();

        foreach (var (enumType, schema) in enumGroup)
        {
            foreach (var enumValue in enumValues)
            {
                if (!enumType.Values.ContainsName(enumValue))
                {
                    context.Log.Write(EnumValuesMismatch(enumType, enumValue, schema));
                }
            }
        }
    }

    private EnumValuesMergeBehavior ResolveMergeBehavior(CompositionContext context)
    {
        if (_enumValuesMergeBehavior is not EnumValuesMergeBehavior.Auto)
        {
            return _enumValuesMergeBehavior;
        }

        foreach (var schema in context.SchemaDefinitions)
        {
            if (schema.Features.Get<ConnectorKindMetadata>()?.Kind == "ApolloFederation")
            {
                return EnumValuesMergeBehavior.Union;
            }
        }

        return EnumValuesMergeBehavior.Strict;
    }

    private static bool IsOutputOnlyEnum(string typeName, ImmutableArray<EnumTypeInfo> enumGroup)
    {
        foreach (var (_, schema) in enumGroup)
        {
            if (IsUsedInInputPosition(typeName, schema))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsUsedInInputPosition(string typeName, MutableSchemaDefinition schema)
    {
        foreach (var type in schema.Types)
        {
            switch (type)
            {
                case MutableComplexTypeDefinition complexType:
                    foreach (var field in complexType.Fields)
                    {
                        foreach (var argument in field.Arguments)
                        {
                            if (argument.Type.NamedType().Name == typeName)
                            {
                                return true;
                            }
                        }
                    }

                    break;

                case MutableInputObjectTypeDefinition inputType:
                    foreach (var field in inputType.Fields)
                    {
                        if (field.Type.NamedType().Name == typeName)
                        {
                            return true;
                        }
                    }

                    break;
            }
        }

        foreach (var directiveDefinition in schema.DirectiveDefinitions)
        {
            foreach (var argument in directiveDefinition.Arguments)
            {
                if (argument.Type.NamedType().Name == typeName)
                {
                    return true;
                }
            }
        }

        return false;
    }
}
