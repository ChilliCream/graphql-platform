using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using HotChocolate.ApolloFederation.Constants;
using HotChocolate.ApolloFederation.Descriptors;
using HotChocolate.ApolloFederation.Helpers;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Types.Helpers;
using static HotChocolate.ApolloFederation.ThrowHelper;
using static HotChocolate.ApolloFederation.Constants.WellKnownContextData;
using static HotChocolate.Types.TagHelper;

namespace HotChocolate.ApolloFederation;

internal sealed class FederationTypeInterceptor : TypeInterceptor
{
    private static readonly object _empty = new();

    private static readonly MethodInfo _matches =
        typeof(ReferenceResolverHelper)
            .GetMethod(
                nameof(ReferenceResolverHelper.Matches),
                BindingFlags.Static | BindingFlags.Public)!;

    private static readonly MethodInfo _execute =
        typeof(ReferenceResolverHelper)
            .GetMethod(
                nameof(ReferenceResolverHelper.ExecuteAsync),
                BindingFlags.Static | BindingFlags.Public)!;

    private static readonly MethodInfo _invalid =
        typeof(ReferenceResolverHelper)
            .GetMethod(
                nameof(ReferenceResolverHelper.Invalid),
                BindingFlags.Static | BindingFlags.Public)!;

    private readonly List<ObjectType> _entityTypes = new();
    private IDescriptorContext _context = default!;
    private ITypeInspector _typeInspector = default!;
    private ObjectType _queryType = default!;

    internal override void InitializeContext(
        IDescriptorContext context,
        TypeInitializer typeInitializer,
        TypeRegistry typeRegistry,
        TypeLookup typeLookup,
        TypeReferenceResolver typeReferenceResolver)
    {
        _typeInspector = context.TypeInspector;
        _context = context;
        ModifyOptions(context, o => o.Mode = TagMode.ApolloFederation);
    }

    public override void OnAfterInitialize(
        ITypeDiscoveryContext discoveryContext,
        DefinitionBase definition)
    {
        if (discoveryContext.Type is ObjectType objectType &&
            definition is ObjectTypeDefinition objectTypeDefinition)
        {
            ApplyMethodLevelReferenceResolvers(
                objectType,
                objectTypeDefinition);

            AddToUnionIfHasTypeLevelKeyDirective(
                objectType,
                objectTypeDefinition);

            AggregatePropertyLevelKeyDirectives(
                objectType,
                objectTypeDefinition,
                discoveryContext);
        }
    }

    internal override void OnAfterResolveRootType(
        ITypeCompletionContext completionContext,
        ObjectTypeDefinition definition,
        OperationType operationType)
    {
        if (operationType is OperationType.Query)
        {
            _queryType = (ObjectType) completionContext.Type;
        }
    }

    public override void OnTypesInitialized()
    {
        if (_entityTypes.Count == 0)
        {
            throw EntityType_NoEntities();
        }
    }

    public override void OnBeforeCompleteType(
        ITypeCompletionContext completionContext,
        DefinitionBase definition)
    {
        AddMemberTypesToTheEntityUnionType(
            completionContext,
            definition);

        AddServiceTypeToQueryType(
            completionContext,
            definition);

        ApplyFederationDirectives(definition);
    }

    public override void OnAfterCompleteType(
        ITypeCompletionContext completionContext,
        DefinitionBase definition)
    {
        if (completionContext.Type is ObjectType type &&
            definition is ObjectTypeDefinition typeDef)
        {
            CompleteExternalFieldSetters(type, typeDef);
            CompleteReferenceResolver(typeDef);
        }
    }

    private void CompleteExternalFieldSetters(ObjectType type, ObjectTypeDefinition typeDef)
        => ExternalSetterExpressionHelper.TryAddExternalSetter(type, typeDef);

    private void CompleteReferenceResolver(ObjectTypeDefinition typeDef)
    {
        if (!typeDef.GetContextData().TryGetValue(EntityResolver, out var resolversObject))
        {
            return;
        }

        if (resolversObject is not IReadOnlyList<ReferenceResolverDefinition> resolvers)
        {
            return;
        }

        if (resolvers.Count == 1)
        {
            typeDef.ContextData[EntityResolver] = resolvers[0].Resolver;
        }
        else
        {
            var expressions = new Stack<(Expression Condition, Expression Execute)>();
            var context = Expression.Parameter(typeof(IResolverContext));

            foreach (var resolverDef in resolvers)
            {
                Expression required = Expression.Constant(resolverDef.Required);
                Expression resolver = Expression.Constant(resolverDef.Resolver);
                Expression condition = Expression.Call(_matches, context, required);
                Expression execute = Expression.Call(_execute, context, resolver);
                expressions.Push((condition, execute));
            }

            Expression current = Expression.Call(_invalid, context);
            var variable = Expression.Variable(typeof(ValueTask<object?>));

            while (expressions.Count > 0)
            {
                var expression = expressions.Pop();
                current = Expression.IfThenElse(
                    expression.Condition,
                    Expression.Assign(variable, expression.Execute),
                    current);
            }

            current = Expression.Block(new[] { variable }, current, variable);

            typeDef.ContextData[EntityResolver] =
                Expression.Lambda<FieldResolverDelegate>(current, context).Compile();
        }
    }

    private void AddServiceTypeToQueryType(
        ITypeCompletionContext completionContext,
        DefinitionBase? definition)
    {
        if (!ReferenceEquals(completionContext.Type, _queryType))
        {
            return;
        }

        var objectTypeDefinition = (ObjectTypeDefinition)definition!;

        var serviceFieldDescriptor = ObjectFieldDescriptor.New(
            _context,
            WellKnownFieldNames.Service);
        serviceFieldDescriptor
            .Type<NonNullType<ServiceType>>()
            .Resolve(_empty);
        objectTypeDefinition.Fields.Add(serviceFieldDescriptor.CreateDefinition());

        var entitiesFieldDescriptor = ObjectFieldDescriptor.New(
            _context,
            WellKnownFieldNames.Entities);
        entitiesFieldDescriptor
            .Type<NonNullType<ListType<EntityType>>>()
            .Argument(
                WellKnownArgumentNames.Representations,
                descriptor => descriptor.Type<NonNullType<ListType<NonNullType<AnyType>>>>())
            .Resolve(
                c => EntitiesResolver.ResolveAsync(
                    c.Schema,
                    c.ArgumentValue<IReadOnlyList<Representation>>(
                        WellKnownArgumentNames.Representations),
                    c));
        objectTypeDefinition.Fields.Add(entitiesFieldDescriptor.CreateDefinition());
    }

    private void ApplyMethodLevelReferenceResolvers(
        ObjectType objectType,
        ObjectTypeDefinition objectTypeDefinition)
    {
        if (objectType.RuntimeType == typeof(object))
        {
            return;
        }

        var descriptor = ObjectTypeDescriptor.From(_context, objectTypeDefinition);

        foreach (var possibleReferenceResolver in
            objectType.RuntimeType.GetMethods(BindingFlags.Static | BindingFlags.Public))
        {
            if (!possibleReferenceResolver.IsDefined(typeof(ReferenceResolverAttribute)))
            {
                continue;
            }

            foreach (var attribute in possibleReferenceResolver.GetCustomAttributes(true))
            {
                if (attribute is ReferenceResolverAttribute casted)
                {
                    casted.TryConfigure(_context, descriptor, possibleReferenceResolver);
                }
            }
        }

        descriptor.CreateDefinition();
    }

    private void AddToUnionIfHasTypeLevelKeyDirective(
        ObjectType objectType,
        ObjectTypeDefinition objectTypeDefinition)
    {
        if (objectTypeDefinition.Directives.Any(
                d => d.Value is DirectiveNode { Name.Value: WellKnownTypeNames.Key }) ||
            objectTypeDefinition.Fields.Any(f => f.ContextData.ContainsKey(WellKnownTypeNames.Key)))
        {
            _entityTypes.Add(objectType);
        }
    }

    private void AggregatePropertyLevelKeyDirectives(
        ObjectType objectType,
        ObjectTypeDefinition objectTypeDefinition,
        ITypeDiscoveryContext discoveryContext)
    {
        // if we find key markers on our fields, we need to construct the key directive
        // from the annotated fields.
        {
            bool foundMarkers = objectTypeDefinition.Fields
                .Any(f => f.ContextData.ContainsKey(KeyMarker));
            if (!foundMarkers)
            {
                return;
            }
        }

        IReadOnlyList<ObjectFieldDefinition> fields = objectTypeDefinition.Fields;
        var fieldSet = new StringBuilder();

        foreach (var fieldDefinition in fields)
        {
            if (fieldDefinition.ContextData.ContainsKey(KeyMarker))
            {
                if (fieldSet.Length > 0)
                {
                    fieldSet.Append(' ');
                }

                fieldSet.Append(fieldDefinition.Name);
            }
        }

        // add the key directive with the dynamically generated field set.
        AddKeyDirective(objectTypeDefinition, fieldSet.ToString());

        // register dependency to the key directive so that it is completed before
        // we complete this type.
        foreach (var directiveDefinition in objectTypeDefinition.Directives)
        {
            discoveryContext.Dependencies.Add(
                new TypeDependency(
                    directiveDefinition.Type,
                    TypeDependencyFulfilled.Completed));

            discoveryContext.Dependencies.Add(new(directiveDefinition.Type));
        }

        // since this type has now a key directive we also need to add this type to
        // the _Entity union type.
        _entityTypes.Add(objectType);
    }

    private void AddMemberTypesToTheEntityUnionType(
        ITypeCompletionContext completionContext,
        DefinitionBase? definition)
    {
        if (completionContext.Type is EntityType &&
            definition is UnionTypeDefinition unionTypeDefinition)
        {
            foreach (var objectType in _entityTypes)
            {
                unionTypeDefinition.Types.Add(TypeReference.Create(objectType));
            }
        }
    }

    private static void AddKeyDirective(
        ObjectTypeDefinition objectTypeDefinition,
        string fieldSet)
    {
        var directiveNode = new DirectiveNode(
            WellKnownTypeNames.Key,
            new ArgumentNode(
                WellKnownArgumentNames.Fields,
                fieldSet));

        objectTypeDefinition.Directives.Add(
            new DirectiveDefinition(directiveNode));
    }

    private enum ApplicableToDefinition
    {
        Enum,
        EnumValue = Enum + 1,

        Interface,
        InterfaceField = Interface + 1,

        InputObject,
        InputField = InputObject + 1,

        Object,
        ObjectField = Object + 1,

        Union,

        Count = Union,
    }

    private struct ApplicableToDefinitionValues<T>
    {
        public readonly T[] Values;

        public ApplicableToDefinitionValues(T[] values)
        {
            Values = values;
        }

        public ref T Enum => ref Values[(int)ApplicableToDefinition.Enum];
        public ref T EnumValue => ref Values[(int)ApplicableToDefinition.EnumValue];
        public ref T Interface => ref Values[(int)ApplicableToDefinition.Interface];
        public ref T InterfaceField => ref Values[(int)ApplicableToDefinition.InterfaceField];
        public ref T InputObject => ref Values[(int)ApplicableToDefinition.InputObject];
        public ref T InputField => ref Values[(int)ApplicableToDefinition.InputField];
        public ref T Object => ref Values[(int)ApplicableToDefinition.Object];
        public ref T ObjectField => ref Values[(int)ApplicableToDefinition.ObjectField];
        public ref T Union => ref Values[(int)ApplicableToDefinition.Union];
        public ref T this[ApplicableToDefinition index] => ref Values[(int)index];
    }

    private static ApplicableToDefinitionValues<T> CreateApplicable<T>()
    {
        return new(new T[(int)ApplicableToDefinition.Count]);
    }

    private static readonly ApplicableToDefinitionValues<ApplicableApolloAttributes> _applicableAttributesByType = CreateDefaultApplicable();

    private static ApplicableToDefinitionValues<ApplicableApolloAttributes> CreateDefaultApplicable()
    {
        var result = CreateApplicable<ApplicableApolloAttributes>();

        var basic = new ApplicableApolloAttributes
        {
            ApolloTag = true,
            Inaccessible = true,
        };
        var authentication = new ApplicableApolloAttributes
        {
            ApolloAuthenticated = true,
            RequiresScopes = true,
        };

        result.Enum = basic | authentication;
        result.EnumValue = basic;

        result.Interface = basic | authentication;
        result.InterfaceField = result.Interface;

        result.InputObject = basic;
        result.InputField = basic;

        result.Object = new()
        {
            All = true,
        };
        result.ObjectField = result.Object;

        result.Union = basic;
        return result;
    }


    /// <summary>
    /// Apply Apollo Federation directives based on the custom attributes.
    /// </summary>
    /// <param name="definition">
    /// Type definition
    /// </param>
    private void ApplyFederationDirectives(DefinitionBase? definition)
    {
        void Apply(
            IHasDirectiveDefinition def,
            MemberInfo target,
            ApplicableToDefinition applicable,
            IEnumerable<(IHasDirectiveDefinition, MemberInfo?)> children)
        {
            {
                var attributesToApply = _applicableAttributesByType[applicable];
                ApplyAttributes(def, target, attributesToApply);
            }

            {
                var attributesToApply = _applicableAttributesByType[applicable + 1];
                foreach (var (childDefinition, childTarget) in children)
                {
                    if (childTarget is not null)
                    {
                        ApplyAttributes(childDefinition, childTarget, attributesToApply);
                    }
                }
            }
        }

        switch (definition)
        {
            case EnumTypeDefinition enumTypeDefinition:
            {
                Apply(
                    enumTypeDefinition,
                    enumTypeDefinition.RuntimeType,
                    ApplicableToDefinition.Enum,
                    enumTypeDefinition.Values
                        .Select(v => ((IHasDirectiveDefinition) v, v.Member)));
                break;
            }
            case InterfaceTypeDefinition interfaceTypeDefinition:
            {
                Apply(
                    interfaceTypeDefinition,
                    interfaceTypeDefinition.RuntimeType,
                    ApplicableToDefinition.Interface,
                    interfaceTypeDefinition.Fields
                        .Select(f => ((IHasDirectiveDefinition) f, f.Member)));
                break;
            }
            case InputObjectTypeDefinition inputObjectTypeDefinition:
            {
                Apply(
                    inputObjectTypeDefinition,
                    inputObjectTypeDefinition.RuntimeType,
                    ApplicableToDefinition.Interface,
                    inputObjectTypeDefinition.Fields
                        .Select(f => ((IHasDirectiveDefinition) f, (MemberInfo?) f.Property)));
                break;
            }
            case ObjectTypeDefinition objectTypeDefinition:
            {
                Apply(
                    objectTypeDefinition,
                    objectTypeDefinition.RuntimeType,
                    ApplicableToDefinition.Object,
                    objectTypeDefinition.Fields
                        .Select(f => ((IHasDirectiveDefinition) f, f.Member)));
                break;
            }
            case UnionTypeDefinition unionTypeDefinition:
            {
                ApplyAttributes(
                    unionTypeDefinition,
                    unionTypeDefinition.RuntimeType,
                    _applicableAttributesByType.Union);
                break;
            }
            default:
                break;
        }
    }

    [Flags]
    private enum ApolloAttributeFlags
    {
        ApolloTag = 1 << 0,
        ApolloAuthenticated = 1 << 1,
        Inaccessible = 1 << 2,
        RequiresScopes = 1 << 3,
        Shareable = 1 << 4,
        All = ApolloTag | ApolloAuthenticated | Inaccessible | RequiresScopes | Shareable,
    }

    private struct ApplicableApolloAttributes
    {
        public ApolloAttributeFlags Flags;

        private readonly bool Get(ApolloAttributeFlags flag)
            => (Flags & flag) == flag;
        private void Set(ApolloAttributeFlags flag, bool set)
        {
            if (set)
            {
                Flags |= flag;
            }
            else
            {
                Flags &= ~flag;
            }
        }

        public static ApplicableApolloAttributes operator |(ApplicableApolloAttributes left, ApplicableApolloAttributes right)
        {
            return new()
            {
                Flags = left.Flags | right.Flags,
            };
        }

        public bool ApolloTag
        {
            readonly get => Get(ApolloAttributeFlags.ApolloTag);
            set => Set(ApolloAttributeFlags.ApolloTag, value);
        }
        public bool ApolloAuthenticated
        {
            readonly get => Get(ApolloAttributeFlags.ApolloAuthenticated);
            set => Set(ApolloAttributeFlags.ApolloAuthenticated, value);
        }
        public bool Inaccessible
        {
            readonly get => Get(ApolloAttributeFlags.Inaccessible);
            set => Set(ApolloAttributeFlags.Inaccessible, value);
        }
        public bool RequiresScopes
        {
            readonly get => Get(ApolloAttributeFlags.RequiresScopes);
            set => Set(ApolloAttributeFlags.RequiresScopes, value);
        }
        public bool Shareable
        {
            readonly get => Get(ApolloAttributeFlags.Shareable);
            set => Set(ApolloAttributeFlags.Shareable, value);
        }
        public bool All
        {
            readonly get => Get(ApolloAttributeFlags.All);
            set => Set(ApolloAttributeFlags.All, value);
        }
    }

    private void ApplyAttributes(
        IHasDirectiveDefinition definition,
        MemberInfo target,
        ApplicableApolloAttributes applicable)
    {
        var customAttributes = target.GetCustomAttributes(inherit: true);

        List<List<Scope>>? requiredScopes = null;

        void AddDirective<T>(T directive)
            where T : class
        {
            definition.AddDirective(directive, _typeInspector);
        }

        foreach (var attribute in customAttributes)
        {
            switch (attribute)
            {
                case ApolloTagAttribute tag
                    when applicable.ApolloTag:
                {
                    AddDirective(new TagValue(tag.Name));
                    break;
                }
                case ApolloAuthenticatedAttribute
                    when applicable.ApolloAuthenticated:
                {
                    AddDirective(WellKnownTypeNames.AuthenticatedDirective);
                    break;
                }
                case InaccessibleAttribute
                    when applicable.Inaccessible:
                {
                    AddDirective(WellKnownTypeNames.Inaccessible);
                    break;
                }
                case RequiresScopesAttribute scopes
                    when applicable.RequiresScopes:
                {
                    var addedScopes = scopes.Scopes
                        .Select(scope => new Scope(scope))
                        .ToList();
                    (requiredScopes ??= new()).Add(addedScopes);
                    break;
                }
                case ShareableAttribute
                    when applicable.Shareable:
                {
                    AddDirective(WellKnownTypeNames.Shareable);
                    break;
                }
            }
        }

        if (requiredScopes is not null)
        {
            AddDirective(new RequiresScopes(requiredScopes));
        }
    }
}
