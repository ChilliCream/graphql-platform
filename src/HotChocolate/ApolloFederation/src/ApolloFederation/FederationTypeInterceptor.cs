using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using HotChocolate.ApolloFederation.Resolvers;
using HotChocolate.ApolloFederation.Types;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Configurations;
using HotChocolate.Types.Helpers;
using HotChocolate.Types.Pagination;
using static HotChocolate.ApolloFederation.ThrowHelper;
using static HotChocolate.Types.TagHelper;

namespace HotChocolate.ApolloFederation;

internal sealed class FederationTypeInterceptor : TypeInterceptor
{
    private static readonly MethodInfo s_matches =
        typeof(ReferenceResolverHelper)
            .GetMethod(
                nameof(ReferenceResolverHelper.Matches),
                BindingFlags.Static | BindingFlags.Public)!;

    private static readonly MethodInfo s_execute =
        typeof(ReferenceResolverHelper)
            .GetMethod(
                nameof(ReferenceResolverHelper.ExecuteAsync),
                BindingFlags.Static | BindingFlags.Public)!;

    private static readonly MethodInfo s_invalid =
        typeof(ReferenceResolverHelper)
            .GetMethod(
                nameof(ReferenceResolverHelper.Invalid),
                BindingFlags.Static | BindingFlags.Public)!;

    private readonly List<ObjectType> _entityTypes = [];
    private readonly Dictionary<Uri, HashSet<string>> _imports = [];
    private IDescriptorContext _context = null!;
    private ITypeInspector _typeInspector = null!;
    private TypeRegistry _typeRegistry = null!;
    private ObjectType _queryType = null!;
    private ExtendedTypeDirectiveReference _keyDirectiveReference = null!;
    private SchemaTypeConfiguration _schemaTypeCfg = null!;
    private RegisteredType _schemaType = null!;
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
            return;
        }

        if (discoveryContext.Type is InterfaceType interfaceType &&
            configuration is InterfaceTypeConfiguration interfaceTypeCfg)
        {
            ApplyMethodLevelReferenceResolvers(
                interfaceType,
                interfaceTypeCfg);

            AggregatePropertyLevelKeyDirectives(
                interfaceType,
                interfaceTypeCfg,
                discoveryContext);
            return;
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

        if (discoveryContexts.Any(t => t.Type is PageInfoType)
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
        if (!_context.Features.TryGet(out ExportedDirectives? exportedDirectives))
        {
            return;
        }

        var composeDirectives = new List<ComposeDirective>();
        foreach (var exportedDirective in exportedDirectives.Directives)
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
            return;
        }

        if (completionContext.Type is InterfaceType interfaceType &&
            configuration is InterfaceTypeConfiguration interfaceTypeCfg)
        {
            CompleteExternalFieldSetters(interfaceType, interfaceTypeCfg);
            CompleteReferenceResolver(interfaceTypeCfg);
            return;
        }
    }

    private void CompleteExternalFieldSetters(ObjectType type, ObjectTypeConfiguration typeCfg)
        => ExternalSetterExpressionHelper.TryAddExternalSetter(type, typeCfg);

    private void CompleteExternalFieldSetters(InterfaceType type, InterfaceTypeConfiguration typeCfg)
        => ExternalSetterExpressionHelper.TryAddExternalSetter(type, typeCfg);

    private void CompleteReferenceResolver(ObjectTypeConfiguration typeCfg)
    {
        var resolvers = typeCfg.Features.Get<List<ReferenceResolverConfiguration>>();

        if (resolvers is null)
        {
            return;
        }

        if (resolvers.Count == 1)
        {
            typeCfg.Features.Set(new ReferenceResolver(resolvers[0].Resolver));
        }
        else
        {
            var expressions = new Stack<(Expression Condition, Expression Execute)>();
            var context = Expression.Parameter(typeof(IResolverContext));

            foreach (var resolverDef in resolvers)
            {
                Expression required = Expression.Constant(resolverDef.Required);
                Expression resolver = Expression.Constant(resolverDef.Resolver);
                Expression condition = Expression.Call(s_matches, context, required);
                Expression execute = Expression.Call(s_execute, context, resolver);
                expressions.Push((condition, execute));
            }

            Expression current = Expression.Call(s_invalid, context);
            var variable = Expression.Variable(typeof(ValueTask<object?>));

            while (expressions.Count > 0)
            {
                var expression = expressions.Pop();
                current = Expression.IfThenElse(
                    expression.Condition,
                    Expression.Assign(variable, expression.Execute),
                    current);
            }

            current = Expression.Block([variable], current, variable);

            typeCfg.Features.Set(
                new ReferenceResolver(
                    Expression.Lambda<FieldResolverDelegate>(current, context).Compile()));
        }
    }

    private void CompleteReferenceResolver(InterfaceTypeConfiguration typeCfg)
    {
        var resolvers = typeCfg.Features.Get<List<ReferenceResolverConfiguration>>();

        if (resolvers is null)
        {
            return;
        }

        if (resolvers.Count == 1)
        {
            typeCfg.Features.Set(new ReferenceResolver(resolvers[0].Resolver));
        }
        else
        {
            var expressions = new Stack<(Expression Condition, Expression Execute)>();
            var context = Expression.Parameter(typeof(IResolverContext));

            foreach (var resolverDef in resolvers)
            {
                Expression required = Expression.Constant(resolverDef.Required);
                Expression resolver = Expression.Constant(resolverDef.Resolver);
                Expression condition = Expression.Call(s_matches, context, required);
                Expression execute = Expression.Call(s_execute, context, resolver);
                expressions.Push((condition, execute));
            }

            Expression current = Expression.Call(s_invalid, context);
            var variable = Expression.Variable(typeof(ValueTask<object?>));

            while (expressions.Count > 0)
            {
                var expression = expressions.Pop();
                current = Expression.IfThenElse(
                    expression.Condition,
                    Expression.Assign(variable, expression.Execute),
                    current);
            }

            current = Expression.Block([variable], current, variable);

            typeCfg.Features.Set(
                new ReferenceResolver(
                    Expression.Lambda<FieldResolverDelegate>(current, context).Compile()));
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

    private void ApplyMethodLevelReferenceResolvers(
        InterfaceType interfaceType,
        InterfaceTypeConfiguration interfaceTypeCfg)
    {
        if (interfaceType.RuntimeType == typeof(object))
        {
            return;
        }

        var descriptor = InterfaceTypeDescriptor.From(_context, interfaceTypeCfg);

        // Static methods won't end up in the schema as fields.
        // The default initialization system only considers instance methods,
        // so we have to handle the attributes for those manually.
        var potentiallyUnregisteredReferenceResolvers = interfaceType.RuntimeType
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

        if (objectTypeCfg.Fields.Any(f => f.Features.TryGet(out KeyMarker? key) && key.Resolvable))
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
        var foundMarkers = objectTypeCfg.Fields.Any(f => f.Features.TryGet(out KeyMarker? _));

        if (!foundMarkers)
        {
            return;
        }

        IReadOnlyList<ObjectFieldConfiguration> fields = objectTypeCfg.Fields;
        var fieldSet = new StringBuilder();
        bool? resolvable = null;

        foreach (var fieldDefinition in fields)
        {
            if (fieldDefinition.Features.TryGet(out KeyMarker? key))
            {
                if (resolvable is null)
                {
                    resolvable = key.Resolvable;
                }
                else if (resolvable != key.Resolvable)
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

    private void AggregatePropertyLevelKeyDirectives(
        InterfaceType interfaceType,
        InterfaceTypeConfiguration interfaceTypeCfg,
        ITypeDiscoveryContext discoveryContext)
    {
        // if we find key markers on our fields, we need to construct the key directive
        // from the annotated fields.
        var foundMarkers = interfaceTypeCfg.Fields.Any(f => f.Features.TryGet(out KeyMarker? _));

        if (!foundMarkers)
        {
            return;
        }

        IReadOnlyList<InterfaceFieldConfiguration> fields = interfaceTypeCfg.Fields;
        var fieldSet = new StringBuilder();
        bool? resolvable = null;

        foreach (var fieldDefinition in fields)
        {
            if (fieldDefinition.Features.TryGet(out KeyMarker? key))
            {
                if (resolvable is null)
                {
                    resolvable = key.Resolvable;
                }
                else if (resolvable != key.Resolvable)
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
        AddKeyDirective(interfaceTypeCfg, fieldSet.ToString(), resolvable ?? true);

        // register dependency to the key directive so that it is completed before
        // we complete this type.
        foreach (var directiveDefinition in interfaceTypeCfg.Directives)
        {
            discoveryContext.Dependencies.Add(
                new TypeDependency(
                    directiveDefinition.Type,
                    TypeDependencyFulfilled.Completed));

            discoveryContext.Dependencies.Add(new(directiveDefinition.Type));
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

    private void AddKeyDirective(
        InterfaceTypeConfiguration interfaceTypeCfg,
        string fieldSet,
        bool resolvable)
    {
        interfaceTypeCfg.Directives.Add(
            new DirectiveConfiguration(
                new KeyDirective(fieldSet, resolvable),
                _keyDirectiveReference));
    }
}
