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
    private SchemaTypeConfiguration _schemaTypeCfg = default!;
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
        TypeSystemConfiguration configuration)
    {
        if (discoveryContext.Type is ObjectType objectType &&
            configuration is ObjectTypeConfiguration objectTypeCfg)
        {
            ApplyMethodLevelReferenceResolvers(
                objectType,
                objectTypeCfg);

            AddToUnionIfHasTypeLevelKeyDirective(
                objectType,
                objectTypeCfg);

            AggregatePropertyLevelKeyDirectives(
                objectType,
                objectTypeCfg,
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
        TypeSystemConfiguration configuration)
    {
        if (configuration is SchemaTypeConfiguration schemaCfg)
        {
            _schemaType = (RegisteredType)completionContext;
            _schemaTypeCfg = schemaCfg;
        }
    }

    public override void OnAfterCompleteName(
        ITypeCompletionContext completionContext,
        TypeSystemConfiguration configuration)
    {
        if (_context.GetFederationVersion() == FederationVersion.Federation10
            || configuration is not ITypeConfiguration and not DirectiveTypeConfiguration)
        {
            return;
        }

        // if we find a PagingInfo we will make all fields sharable.
        if (configuration is ObjectTypeConfiguration typeCfg
            && typeCfg.Name.Equals(PageInfoType.Names.PageInfo))
        {
            foreach (var fieldCfg in typeCfg.Fields)
            {
                if (fieldCfg.Directives.All(t => t.Value is not ShareableDirective))
                {
                    var typeRef = TypeReference.CreateDirective(_typeInspector.GetType(typeof(ShareableDirective)));
                    fieldCfg.Directives.Add(new DirectiveConfiguration(ShareableDirective.Default, typeRef));
                }
            }
        }

        var hasRuntimeType = (IHasRuntimeType)configuration;
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

        _schemaTypeCfg
            .GetLegacyConfiguration()
            .AddDirective(
                new LinkDirective(version.ToUrl(), federationTypes),
                _typeInspector);

        foreach (var import in _imports)
        {
            if (import.Key.TryToVersion(out _))
            {
                continue;
            }

            _schemaTypeCfg
                .GetLegacyConfiguration()
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
                composeDirectives.Add(new ComposeDirective($"@{exportedDirectiveType.Type.Name}"));
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
                _schemaTypeCfg
                    .GetLegacyConfiguration()
                    .AddDirective(directive, _typeInspector);
            }
        }
    }

    public override void OnAfterResolveRootType(
        ITypeCompletionContext completionContext,
        ObjectTypeConfiguration configuration,
        OperationType operationType)
    {
        if (operationType is OperationType.Query)
        {
            _queryType = (ObjectType)completionContext.Type;
        }
    }

    public override void OnBeforeCompleteType(
        ITypeCompletionContext completionContext,
        TypeSystemConfiguration configuration)
    {
        AddMemberTypesToTheEntityUnionType(
            completionContext,
            configuration);

        AddServiceTypeToQueryType(
            completionContext,
            configuration);
    }

    public override void OnAfterMakeExecutable(
        ITypeCompletionContext completionContext,
        TypeSystemConfiguration configuration)
    {
        if (completionContext.Type is ObjectType type &&
            configuration is ObjectTypeConfiguration typeCfg)
        {
            CompleteExternalFieldSetters(type, typeCfg);
            CompleteReferenceResolver(typeCfg);
        }
    }

    private void CompleteExternalFieldSetters(ObjectType type, ObjectTypeConfiguration typeCfg)
        => ExternalSetterExpressionHelper.TryAddExternalSetter(type, typeCfg);

    private void CompleteReferenceResolver(ObjectTypeConfiguration typeCfg)
    {
        IReadOnlyList<ReferenceResolverConfiguration> resolvers;
        {
            var contextData = typeCfg.GetFeatures();

            if (!contextData.TryGetValue(EntityResolver, out var resolversObject))
            {
                return;
            }

            if (resolversObject is not IReadOnlyList<ReferenceResolverConfiguration> r)
            {
                return;
            }

            resolvers = r;
        }

        if (resolvers.Count == 1)
        {
            typeCfg.Features[EntityResolver] = resolvers[0].Resolver;
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

            typeCfg.Features[EntityResolver] =
                Expression.Lambda<FieldResolverDelegate>(current, context).Compile();
        }
    }

    private void AddServiceTypeToQueryType(
        ITypeCompletionContext completionContext,
        TypeSystemConfiguration? definition)
    {
        if (!ReferenceEquals(completionContext.Type, _queryType))
        {
            return;
        }

        var objectTypeCfg = (ObjectTypeConfiguration)definition!;
        objectTypeCfg.Fields.Add(ServerFields.CreateServiceField(_context));
        if (_entityTypes.Count > 0)
        {
            objectTypeCfg.Fields.Add(ServerFields.CreateEntitiesField(_context));
        }
    }

    private void ApplyMethodLevelReferenceResolvers(
        ObjectType objectType,
        ObjectTypeConfiguration objectTypeCfg)
    {
        if (objectType.RuntimeType == typeof(object))
        {
            return;
        }

        var descriptor = ObjectTypeDescriptor.From(_context, objectTypeCfg);

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
        descriptor.CreateConfiguration();
    }

    private void AddToUnionIfHasTypeLevelKeyDirective(
        ObjectType objectType,
        ObjectTypeConfiguration objectTypeCfg)
    {
        if (objectTypeCfg.Directives.FirstOrDefault(d => d.Value is KeyDirective) is { } keyDirective &&
            ((KeyDirective)keyDirective.Value).Resolvable)
        {
            _entityTypes.Add(objectType);
            return;
        }

        if (objectTypeCfg.Fields.Any(f => f.Features.TryGetValue(KeyMarker, out var resolvable) &&
                resolvable is true))
        {
            _entityTypes.Add(objectType);
        }
    }

    private void AggregatePropertyLevelKeyDirectives(
        ObjectType objectType,
        ObjectTypeConfiguration objectTypeCfg,
        ITypeDiscoveryContext discoveryContext)
    {
        // if we find key markers on our fields, we need to construct the key directive
        // from the annotated fields.
        {
            var foundMarkers = objectTypeCfg.Fields.Any(f => f.Features.ContainsKey(KeyMarker));

            if (!foundMarkers)
            {
                return;
            }
        }

        IReadOnlyList<ObjectFieldConfiguration> fields = objectTypeCfg.Fields;
        var fieldSet = new StringBuilder();
        bool? resolvable = null;

        foreach (var fieldDefinition in fields)
        {
            if (fieldDefinition.Features.TryGetValue(KeyMarker, out var value) &&
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
        AddKeyDirective(objectTypeCfg, fieldSet.ToString(), resolvable ?? true);

        // register dependency to the key directive so that it is completed before
        // we complete this type.
        foreach (var directiveDefinition in objectTypeCfg.Directives)
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
        TypeSystemConfiguration? definition)
    {
        if (completionContext.Type is _EntityType &&
            definition is UnionTypeConfiguration unionTypeCfg)
        {
            foreach (var objectType in _entityTypes)
            {
                unionTypeCfg.Types.Add(TypeReference.Create(objectType));
            }
        }
    }

    private void AddKeyDirective(
        ObjectTypeConfiguration objectTypeCfg,
        string fieldSet,
        bool resolvable)
    {
        objectTypeCfg.Directives.Add(
            new DirectiveConfiguration(
                new KeyDirective(fieldSet, resolvable),
                _keyDirectiveReference));
    }
}
