using HotChocolate.Fusion.Events;
using HotChocolate.Fusion.Events.Contracts;
using HotChocolate.Fusion.Extensions;
using HotChocolate.Fusion.Features;
using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using static HotChocolate.Fusion.Logging.LogEntryHelper;
using ArgumentNames = HotChocolate.Fusion.WellKnownArgumentNames;
using DirectiveNames = HotChocolate.Fusion.WellKnownDirectiveNames;

namespace HotChocolate.Fusion.PostMergeValidationRules;

/// <summary>
/// A field declared on a type that its source schema cannot reach from its root types must be
/// resolved by another source schema whenever clients can reach the type in the composite schema.
/// Otherwise, composition fails with an <c>UNREACHABLE_SOURCE_FIELD</c> error.
/// </summary>
internal sealed class UnreachableSourceFieldRule : IEventHandler<SchemaEvent>
{
    public void Handle(SchemaEvent @event, CompositionContext context)
    {
        var mergedSchema = @event.Schema;
        AccessibleTypes? accessibleTypes = null;

        foreach (var schema in context.SchemaDefinitions)
        {
            if (schema.Features.Get<UnreachableTypesMetadata>() is not { } metadata)
            {
                continue;
            }

            foreach (var type in metadata.Types)
            {
                if (!mergedSchema.Types.TryGetType<MutableComplexTypeDefinition>(type.Name, out var mergedType))
                {
                    continue;
                }

                accessibleTypes ??= AccessibleTypes.Create(mergedSchema);

                // A type that clients cannot reach in the composite schema is removed with all its fields.
                if (!accessibleTypes.Contains(mergedType))
                {
                    continue;
                }

                foreach (var field in type.Fields)
                {
                    // These declarations never contribute a resolvable field to the composite schema.
                    if (field.IsExternal || field.IsInternal || field.IsInaccessible || field.IsOverridden)
                    {
                        continue;
                    }

                    if (mergedType.Fields.TryGetField(field.Name, out var mergedField)
                        && HasResolvingSchema(mergedField))
                    {
                        continue;
                    }

                    // A field of an interface is resolved through the implementations of the interface.
                    if (mergedType is MutableInterfaceTypeDefinition mergedInterfaceType
                        && accessibleTypes.IsResolvedByEveryImplementation(mergedInterfaceType, field.Name))
                    {
                        continue;
                    }

                    context.Log.Write(UnreachableSourceField(field, type, schema));
                }
            }
        }
    }

    private static bool HasResolvingSchema(MutableOutputFieldDefinition mergedField)
    {
        foreach (var directive in mergedField.Directives.AsEnumerable())
        {
            if (directive.Name != DirectiveNames.FusionField)
            {
                continue;
            }

            if (!directive.Arguments.TryGetValue(ArgumentNames.Partial, out var partial)
                || partial.Value is not true)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// The types of a composite schema that clients can reach from its root types through
    /// accessible fields.
    /// </summary>
    private sealed class AccessibleTypes
    {
        private readonly HashSet<ITypeDefinition> _types = [];
        private readonly Dictionary<MutableInterfaceTypeDefinition, List<MutableObjectTypeDefinition>>
            _implementations = [];

        private AccessibleTypes()
        {
        }

        public bool Contains(MutableComplexTypeDefinition type) => _types.Contains(type);

        /// <summary>
        /// Determines whether every accessible object type that implements
        /// <paramref name="interfaceType"/> resolves the field named <paramref name="fieldName"/>.
        /// </summary>
        public bool IsResolvedByEveryImplementation(
            MutableInterfaceTypeDefinition interfaceType,
            string fieldName)
        {
            if (!_implementations.TryGetValue(interfaceType, out var implementations))
            {
                return true;
            }

            foreach (var implementation in implementations)
            {
                if (!_types.Contains(implementation))
                {
                    continue;
                }

                if (!implementation.Fields.TryGetField(fieldName, out var field)
                    || !HasResolvingSchema(field))
                {
                    return false;
                }
            }

            return true;
        }

        public static AccessibleTypes Create(MutableSchemaDefinition schema)
        {
            var accessibleTypes = new AccessibleTypes();
            var implementationsByInterface = accessibleTypes._implementations;

            foreach (var type in schema.Types)
            {
                if (type is not MutableObjectTypeDefinition objectType)
                {
                    continue;
                }

                foreach (var interfaceType in objectType.Implements)
                {
                    if (!implementationsByInterface.TryGetValue(interfaceType, out var implementations))
                    {
                        implementations = [];
                        implementationsByInterface.Add(interfaceType, implementations);
                    }

                    implementations.Add(objectType);
                }
            }

            var expandedInterfaceTypes = new HashSet<MutableInterfaceTypeDefinition>();
            var backlog = new Stack<ITypeDefinition>();

            if (schema.QueryType is not null)
            {
                backlog.Push(schema.QueryType);
            }

            if (schema.MutationType is not null)
            {
                backlog.Push(schema.MutationType);
            }

            if (schema.SubscriptionType is not null)
            {
                backlog.Push(schema.SubscriptionType);
            }

            while (backlog.TryPop(out var type))
            {
                if (type.HasFusionInaccessibleDirective() || !accessibleTypes._types.Add(type))
                {
                    continue;
                }

                switch (type)
                {
                    case MutableComplexTypeDefinition complexType:
                        foreach (var interfaceType in complexType.Implements)
                        {
                            backlog.Push(interfaceType);
                        }

                        foreach (var field in complexType.Fields)
                        {
                            if (field.HasFusionInaccessibleDirective())
                            {
                                continue;
                            }

                            var fieldType = field.Type.AsTypeDefinition();
                            backlog.Push(fieldType);

                            // A field of an interface type can return any implementation of it.
                            if (fieldType is MutableInterfaceTypeDefinition fieldInterfaceType
                                && expandedInterfaceTypes.Add(fieldInterfaceType)
                                && implementationsByInterface.TryGetValue(
                                    fieldInterfaceType,
                                    out var implementations))
                            {
                                foreach (var implementation in implementations)
                                {
                                    backlog.Push(implementation);
                                }
                            }
                        }

                        break;

                    case MutableUnionTypeDefinition unionType:
                        foreach (var memberType in unionType.Types)
                        {
                            backlog.Push(memberType);
                        }

                        break;
                }
            }

            return accessibleTypes;
        }
    }
}
