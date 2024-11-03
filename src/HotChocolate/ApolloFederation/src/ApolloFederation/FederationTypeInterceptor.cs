using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using HotChocolate.ApolloFederation.Resolvers;
using HotChocolate.ApolloFederation.Types;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Types.Helpers;
using HotChocolate.Types.Pagination;
using static HotChocolate.ApolloFederation.FederationContextData;
using static HotChocolate.ApolloFederation.ThrowHelper;
using static HotChocolate.Types.TagHelper;

namespace HotChocolate.ApolloFederation;

internal sealed class FederationTypeInterceptor : TypeInterceptor
{
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

    private readonly List<ObjectType> _entityTypes = [];
    private readonly Dictionary<Uri, HashSet<string>> _imports = new();
    private IDescriptorContext _context = default!;
    private ITypeInspector _typeInspector = default!;
    private TypeRegistry _typeRegistry = default!;
    private ObjectType _queryType = default!;
    private ExtendedTypeDirectiveReference _keyDirectiveReference = default!;
    private SchemaTypeDefinition _schemaTypeDefinition = default!;
    private RegisteredType _schemaType = default!;
    private bool _registeredTypes;

    internal override void InitializeContext(
        IDescriptorContext context,
        TypeInitializer typeInitializer,
        TypeRegistry typeRegistry,
        TypeLookup typeLookup,
        TypeReferenceResolver typeReferenceResolver)
    {
        _typeInspector = context.TypeInspector;
        _context = context;
        _typeRegistry = typeRegistry;
        _keyDirectiveReference = new ExtendedTypeDirectiveReference(_typeInspector.GetType(typeof(KeyDirective)));
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

    public override IEnumerable<TypeReference> RegisterMoreTypes(
        IReadOnlyCollection<ITypeDiscoveryContext> discoveryContexts)
    {
        if (_registeredTypes)
        {
            yield break;
        }

        _registeredTypes = true;
        yield return _typeInspector.GetTypeRef(typeof(_Service));
        if (_entityTypes.Count > 0)
        {
            yield return _typeInspector.GetTypeRef(typeof(_EntityType));
        }
        yield return _typeInspector.GetTypeRef(typeof(_AnyType));
        yield return _typeInspector.GetTypeRef(typeof(FieldSetType));

        if (_context.GetFederationVersion() > FederationVersion.Federation10)
        {
            yield return _typeInspector.GetTypeRef(typeof(LinkDirective));
        }

        if(discoveryContexts.Any(t => t.Type is PageInfoType)
            && discoveryContexts.All(t => t.Type is not DirectiveType<ShareableDirective>))
        {
            yield return _typeInspector.GetTypeRef(typeof(ShareableDirective));
        }
    }

    public override void OnBeforeCompleteName(
        ITypeCompletionContext completionContext,
        DefinitionBase definition)
    {
        if (definition is SchemaTypeDefinition schemaDef)
        {
            _schemaType = (RegisteredType)completionContext;
            _schemaTypeDefinition = schemaDef;
        }
    }

    public override void OnAfterCompleteName(
        ITypeCompletionContext completionContext,
        DefinitionBase definition)
    {
        if (_context.GetFederationVersion() == FederationVersion.Federation10
            || definition is not ITypeDefinition and not DirectiveTypeDefinition)
        {
            return;
        }

        // if we find a PagingInfo we will make all fields sharable.
        if (definition is ObjectTypeDefinition typeDef
            && typeDef.Name.Equals(PageInfoType.Names.PageInfo))
        {
            foreach (var fieldDef in typeDef.Fields)
            {
                if (fieldDef.Directives.All(t => t.Value is not ShareableDirective))
                {
                    var typeRef = TypeReference.CreateDirective(_typeInspector.GetType(typeof(ShareableDirective)));
                    fieldDef.Directives.Add(new DirectiveDefinition(ShareableDirective.Default, typeRef));
                }
            }
        }

        var hasRuntimeType = (IHasRuntimeType)definition;
        var type = hasRuntimeType.RuntimeType;

        if (type != typeof(object) &&
            type.IsDefined(typeof(PackageAttribute)))
        {
            RegisterImport(type);
            return;
        }

        type = completionContext.Type.GetType();

        if (type.IsDefined(typeof(PackageAttribute)))
        {
            RegisterImport(type);
            return;
        }

        if (type == typeof(DirectiveType<Tag>))
        {
            RegisterTagImport();
        }

        return;

        void RegisterImport(MemberInfo element)
        {
            var package = element.GetCustomAttribute<PackageAttribute>();

            if (package is null)
            {
                return;
            }

            if (!_imports.TryGetValue(package.Url, out var types))
            {
                types = [];
                _imports[package.Url] = types;
            }

            if (completionContext.Type is DirectiveType)
            {
                types.Add($"@{completionContext.Type.Name}");
            }
            else
            {
                types.Add(completionContext.Type.Name);
            }
        }

        void RegisterTagImport()
        {
            var packageUrl = FederationVersion.Federation20.ToUrl();

            if (!_imports.TryGetValue(packageUrl, out var types))
            {
                types = [];
                _imports[packageUrl] = types;
            }

            types.Add($"@{completionContext.Type.Name}");
        }
    }

    public override void OnTypesCompletedName()
    {
        RegisterExportedDirectives();
        RegisterImports();
    }

    private void RegisterImports()
    {
        if (_imports.Count == 0)
        {
            return;
        }

        var version = _context.GetFederationVersion();
        var federationTypes = new HashSet<string>();

        foreach (var import in _imports)
        {
            if (!import.Key.TryToVersion(out var importVersion))
            {
                continue;
            }

            if (importVersion > version)
            {
                // todo: throw helper
                throw new SchemaException(
                    SchemaErrorBuilder.New()
                        .SetMessage(
                            "The following federation types were used and are not supported by " +
                            "the current federation version: {0}",
                            string.Join(", ", import.Value))
                        .Build());
            }

            federationTypes.UnionWith(import.Value);
        }

        if (version == FederationVersion.Federation10)
        {
            return;
        }

        var dependency = new TypeDependency(
            _typeInspector.GetTypeRef(typeof(LinkDirective)),
            TypeDependencyFulfilled.Completed);
        _schemaType.Dependencies.Add(dependency);

        _schemaTypeDefinition
            .GetLegacyDefinition()
            .AddDirective(
                new LinkDirective(version.ToUrl(), federationTypes),
                _typeInspector);

        foreach (var import in _imports)
        {
            if (import.Key.TryToVersion(out _))
            {
                continue;
            }

            _schemaTypeDefinition
                .GetLegacyDefinition()
                .AddDirective(
                    new LinkDirective(import.Key, import.Value),
                    _typeInspector);
        }
    }

    private void RegisterExportedDirectives()
    {
        if (!_context.ContextData.TryGetValue(ExportedDirectives, out var value) ||
            value is not List<Type> exportedDirectives)
        {
            return;
        }

        var composeDirectives = new List<ComposeDirective>();
        foreach (var exportedDirective in exportedDirectives)
        {
            var typeReference = _typeInspector.GetTypeRef(exportedDirective);
            if (_typeRegistry.TryGetType(typeReference, out var exportedDirectiveType))
            {
                composeDirectives.Add(new ComposeDirective(exportedDirectiveType.Type.Name));
            }
        }

        if (composeDirectives.Count > 0)
        {
            var dependency = new TypeDependency(
                _typeInspector.GetTypeRef(typeof(ComposeDirective)),
                TypeDependencyFulfilled.Completed);
            _schemaType.Dependencies.Add(dependency);

            foreach (var directive in composeDirectives)
            {
                _schemaTypeDefinition
                    .GetLegacyDefinition()
                    .AddDirective(directive, _typeInspector);
            }
        }
    }

    public override void OnAfterResolveRootType(
        ITypeCompletionContext completionContext,
        ObjectTypeDefinition definition,
        OperationType operationType)
    {
        if (operationType is OperationType.Query)
        {
            _queryType = (ObjectType)completionContext.Type;
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

    internal override void OnAfterCreateSchemaInternal(IDescriptorContext context, ISchema schema) { }

    private void CompleteExternalFieldSetters(ObjectType type, ObjectTypeDefinition typeDef)
        => ExternalSetterExpressionHelper.TryAddExternalSetter(type, typeDef);

    private void CompleteReferenceResolver(ObjectTypeDefinition typeDef)
    {
        IReadOnlyList<ReferenceResolverDefinition> resolvers;
        {
            var contextData = typeDef.GetContextData();

            if (!contextData.TryGetValue(EntityResolver, out var resolversObject))
            {
                return;
            }

            if (resolversObject is not IReadOnlyList<ReferenceResolverDefinition> r)
            {
                return;
            }

            resolvers = r;
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

            current = Expression.Block(new[] { variable, }, current, variable);

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
        objectTypeDefinition.Fields.Add(ServerFields.CreateServiceField(_context));
        if (_entityTypes.Count > 0)
        {
            objectTypeDefinition.Fields.Add(ServerFields.CreateEntitiesField(_context));
        }
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

        // Static methods won't end up in the schema as fields.
        // The default initialization system only considers instance methods,
        // so we have to handle the attributes for those manually.
        var potentiallyUnregisteredReferenceResolvers = objectType.RuntimeType
            .GetMethods(BindingFlags.Static | BindingFlags.Public);

        foreach (var possibleReferenceResolver in potentiallyUnregisteredReferenceResolvers)
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

        // This seems to re-detect the entity resolver and save it into the context data.
        descriptor.CreateDefinition();
    }

    private void AddToUnionIfHasTypeLevelKeyDirective(
        ObjectType objectType,
        ObjectTypeDefinition objectTypeDefinition)
    {
        if (objectTypeDefinition.Directives.FirstOrDefault(d => d.Value is KeyDirective) is { } keyDirective &&
            ((KeyDirective)keyDirective.Value).Resolvable)
        {
            _entityTypes.Add(objectType);
            return;
        }

        if (objectTypeDefinition.Fields.Any(f => f.ContextData.TryGetValue(KeyMarker, out var resolvable) &&
                resolvable is true))
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
            var foundMarkers = objectTypeDefinition.Fields.Any(f => f.ContextData.ContainsKey(KeyMarker));

            if (!foundMarkers)
            {
                return;
            }
        }

        IReadOnlyList<ObjectFieldDefinition> fields = objectTypeDefinition.Fields;
        var fieldSet = new StringBuilder();
        bool? resolvable = null;

        foreach (var fieldDefinition in fields)
        {
            if (fieldDefinition.ContextData.TryGetValue(KeyMarker, out var value) &&
                value is bool currentResolvable)
            {
                if (resolvable is null)
                {
                    resolvable = currentResolvable;
                }
                else if (resolvable != currentResolvable)
                {
                    throw Key_FieldSet_ResolvableMustBeConsistent(fieldDefinition.Member!);
                }

                if (fieldSet.Length > 0)
                {
                    fieldSet.Append(' ');
                }

                fieldSet.Append(fieldDefinition.Name);
            }
        }

        // add the key directive with the dynamically generated field set.
        AddKeyDirective(objectTypeDefinition, fieldSet.ToString(), resolvable ?? true);

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

        if (resolvable ?? true)
        {
            // since this type has now a key directive we also need to add this type to
            // the _Entity union type provided that the key is resolvable.
            _entityTypes.Add(objectType);
        }
    }

    private void AddMemberTypesToTheEntityUnionType(
        ITypeCompletionContext completionContext,
        DefinitionBase? definition)
    {
        if (completionContext.Type is _EntityType &&
            definition is UnionTypeDefinition unionTypeDefinition)
        {
            foreach (var objectType in _entityTypes)
            {
                unionTypeDefinition.Types.Add(TypeReference.Create(objectType));
            }
        }
    }

    private void AddKeyDirective(
        ObjectTypeDefinition objectTypeDefinition,
        string fieldSet,
        bool resolvable)
    {
        objectTypeDefinition.Directives.Add(
            new DirectiveDefinition(
                new KeyDirective(fieldSet, resolvable),
                _keyDirectiveReference));
    }
}
