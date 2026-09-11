using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Configuration;
using HotChocolate.Features;
using HotChocolate.Properties;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Configurations;
using HotChocolate.Types.Helpers;
using HotChocolate.Utilities;
using static HotChocolate.Types.Relay.NodeResolverCompilerHelper;

namespace HotChocolate.Types.Relay.Descriptors;

public abstract class NodeDescriptorBase(IDescriptorContext context)
    : DescriptorBase<NodeConfiguration>(context)
{
    protected internal sealed override NodeConfiguration Configuration { get; protected set; } = new();

    protected abstract IObjectFieldDescriptor ConfigureNodeField();

    /// <inheritdoc cref="INodeDescriptor.ResolveNodeBatch(BatchResolverDelegate)"/>
    public IObjectFieldDescriptor ResolveNodeBatch(BatchResolverDelegate batchResolver)
    {
        ArgumentNullException.ThrowIfNull(batchResolver);
        Configuration.ResolverField ??= new ObjectFieldConfiguration();
        ObjectFieldDescriptor.From(Context, Configuration.ResolverField).ResolveBatch(batchResolver);
        Configuration.ResolverField.Resolver = null;
        return ConfigureNodeField();
    }

    /// <inheritdoc cref="INodeDescriptor.ResolveNodeBatch{TId}"/>
    public IObjectFieldDescriptor ResolveNodeBatch<TId>(BatchNodeResolverDelegate<object, TId> batchResolver)
    {
        ArgumentNullException.ThrowIfNull(batchResolver);
        Configuration.ResolverField ??= new ObjectFieldConfiguration();
        BatchNodeResolverHelper.Configure(Configuration.ResolverField, batchResolver);
        return ConfigureNodeField();
    }

    /// <inheritdoc cref="INodeDescriptor.ResolveNodeBatchWith{TResolver}"/>
    public IObjectFieldDescriptor ResolveNodeBatchWith<TResolver>(Expression<Func<TResolver, object?>> method)
    {
        ArgumentNullException.ThrowIfNull(method);
        var member = method.ExtractMember();
        var resolverField = Configuration.ResolverField ??= new ObjectFieldConfiguration();
        ObjectFieldDescriptor.From(Context, resolverField).ResolveBatchWith(method);
        resolverField.Member = member;
        resolverField.ResolverMember = null;
        resolverField.BatchResolver = null;
        return ConfigureNodeField();
    }

    /// <inheritdoc cref="INodeDescriptor.ResolveNodeBatchWith(MethodInfo)"/>
    public IObjectFieldDescriptor ResolveNodeBatchWith(MethodInfo method)
    {
        ArgumentNullException.ThrowIfNull(method);
        var resolverField = Configuration.ResolverField ??= new ObjectFieldConfiguration();
        ObjectFieldDescriptor.From(Context, resolverField).ResolveBatchWith(method);
        resolverField.Member = method;
        resolverField.ResolverMember = null;
        resolverField.BatchResolver = null;
        return ConfigureNodeField();
    }

    /// <summary>
    /// Specifies a delegate to resolve the node from its id.
    /// </summary>
    /// <param name="fieldResolver">
    /// The delegate to resolve the node from its id.
    /// </param>
    public virtual IObjectFieldDescriptor ResolveNode(
        FieldResolverDelegate fieldResolver)
    {
        Configuration.ResolverField ??= new ObjectFieldConfiguration();
        Configuration.ResolverField.Resolver = fieldResolver ??
            throw new ArgumentNullException(nameof(fieldResolver));

        return ConfigureNodeField();
    }

    /// <summary>
    /// Specifies a delegate to resolve the node from its id.
    /// </summary>
    /// <param name="fieldResolver">
    /// The delegate to resolve the node from its id.
    /// </param>
    public IObjectFieldDescriptor ResolveNode<TId>(
        NodeResolverDelegate<object, TId> fieldResolver)
    {
        ArgumentNullException.ThrowIfNull(fieldResolver);

        return ResolveNode(async ctx =>
        {
            if (ctx.LocalContextData.TryGetValue(
                WellKnownContextData.InternalId,
                out var o) && o is TId id)
            {
                return await fieldResolver(ctx, id).ConfigureAwait(false);
            }

            return null;
        });
    }

    /// <summary>
    /// Specifies a member expression from which the node resolver is compiled from.
    /// </summary>
    /// <param name="method">
    /// The node resolver member expression.
    /// </param>
    /// <typeparam name="TResolver">
    /// The declaring node resolver member type.
    /// </typeparam>
    public IObjectFieldDescriptor ResolveNodeWith<TResolver>(
        Expression<Func<TResolver, object?>> method)
    {
        ArgumentNullException.ThrowIfNull(method);

        var member = method.TryExtractMember();

        if (member is MethodInfo m)
        {
            if (m.IsDefined(typeof(BatchResolverAttribute)))
            {
                return ResolveNodeBatchWith(method);
            }

            Configuration.ResolverField ??= new ObjectFieldConfiguration();
            Configuration.ResolverField.Member = m;
            Configuration.ResolverField.DeclaringType = m.ReflectedType ?? m.DeclaringType;
            Configuration.ResolverField.ResolverType = typeof(TResolver);
            return ConfigureNodeField();
        }

        throw new ArgumentException(
            TypeResources.NodeDescriptor_MustBeMethod,
            nameof(member));
    }

    /// <summary>
    /// Specifies a method from which a node resolver shall be compiled from.
    /// </summary>
    /// <param name="method">
    /// The node resolver method.
    /// </param>
    public IObjectFieldDescriptor ResolveNodeWith(MethodInfo method)
    {
        ArgumentNullException.ThrowIfNull(method);

        if (method.IsDefined(typeof(BatchResolverAttribute)))
        {
            return ResolveNodeBatchWith(method);
        }

        Configuration.ResolverField ??= new ObjectFieldConfiguration();
        Configuration.ResolverField.Member = method;
        Configuration.ResolverField.DeclaringType = method.ReflectedType ?? method.DeclaringType;
        Configuration.ResolverField.ResolverType = method.DeclaringType ?? typeof(object);
        return ConfigureNodeField();
    }

    protected void CompleteResolver(ITypeCompletionContext context, ObjectTypeConfiguration definition)
    {
        var descriptorContext = context.DescriptorContext;

        if (Configuration.ResolverField is not null)
        {
            // we let the descriptor complete on the definition object.
            ObjectFieldDescriptor
                .From(descriptorContext, Configuration.ResolverField)
                .CreateConfiguration();

            if (Configuration.ResolverField.IsBatchResolver)
            {
                var resolverField = Configuration.ResolverField;
                var batchResolver = resolverField.BatchResolver
                    ?? Context.ResolverCompiler.CompileBatchResolve(
                        (MethodInfo)resolverField.Member!,
                        typeof(object),
                        resolverField.ResolverType,
                        parameterExpressionBuilders: [BatchNodeIdParameterExpressionBuilder.Instance]);
                var batchPipeline = ObjectField.CompileBatchPipeline(
                    resolverField.GetBatchMiddlewareDefinitions(),
                    resolverField.GetResultConverters(),
                    batchResolver);
                var directiveDefs = resolverField.GetDirectives();

                if (directiveDefs.Count > 0)
                {
                    var directives = DirectiveCollection.CreateAndComplete(
                        context,
                        DirectiveLocation.FieldDefinition,
                        resolverField,
                        directiveDefs);

                    for (var i = directives.Count - 1; i >= 0; i--)
                    {
                        var directive = directives[i];
                        if (directive.Type.BatchMiddleware is { } middleware)
                        {
                            batchPipeline = middleware(batchPipeline, directive);
                        }
                    }
                }

                definition.Features.GetOrSet<NodeTypeFeature>().NodeResolver = new NodeResolverInfo(
                    null,
                    null,
                    batchPipeline,
                    resolverField.BatchPartitionKeyResolver);
                return;
            }

            // after that all middleware should be available on the field definition, and we can
            // start compiling the resolver and the resolver pipeline.
            if (Configuration.ResolverField.Resolver is null
                && Configuration.ResolverField.Member is not null)
            {
                Configuration.ResolverField.Resolvers =
                    Context.ResolverCompiler.CompileResolve(
                        Configuration.ResolverField.Member,
                        typeof(object),
                        Configuration.ResolverField.ResolverType,
                        parameterExpressionBuilders: ParameterExpressionBuilders);
            }

            if (Configuration.ResolverField.Resolver is not null)
            {
                var pipeline = FieldMiddlewareCompiler.Compile(
                    context.GlobalComponents,
                    Configuration.ResolverField.GetMiddlewareDefinitions(),
                    Configuration.ResolverField.GetResultConverters(),
                    Configuration.ResolverField.Resolver,
                    false);

                var directiveDefs = Configuration.ResolverField.GetDirectives();

                if (directiveDefs.Count > 0)
                {
                    var directives =
                        DirectiveCollection.CreateAndComplete(
                            context,
                            DirectiveLocation.FieldDefinition,
                            Configuration.ResolverField,
                            directiveDefs);

                    foreach (var directive in directives)
                    {
                        if (directive.Type.Middleware is not null)
                        {
                            pipeline = directive.Type.Middleware.Invoke(pipeline, directive);
                        }
                    }
                }

                definition.Features.GetOrSet<NodeTypeFeature>().NodeResolver = new NodeResolverInfo(null, pipeline);
            }
        }
    }

    protected static class ConverterHelper
    {
        public static IObjectFieldDescriptor TryAdd(IObjectFieldDescriptor descriptor)
        {
            var extensions = descriptor.Extend();
            var context = extensions.Context;
            var converters = extensions.Configuration.FormatterConfigurations;
            var formatter = context.Features.GetOrSet(Create, context.NodeIdSerializerAccessor);

            if (!converters.Contains(formatter))
            {
                converters.Add(formatter);
            }

            return descriptor;
        }

        public static ResultFormatterConfiguration Create(
            INodeIdSerializerAccessor serializerAccessor)
            => new((context, result)
                    => result is not null
                        ? serializerAccessor.Serializer.Format(context.ObjectType.Name, result)
                        : null,
                isRepeatable: false,
                key: WellKnownMiddleware.GlobalId);
    }
}

internal static class BatchNodeResolverHelper
{
    internal static void Configure<TNode, TId>(
        ObjectFieldConfiguration configuration,
        BatchNodeResolverDelegate<TNode, TId> batchResolver)
    {
        configuration.SetBatchResolverFlags();
        configuration.Resolver = null;
        configuration.BatchResolver = async contexts =>
        {
            var ids = new TId[contexts.Length];
            for (var i = 0; i < contexts.Length; i++)
            {
                ids[i] = (TId)contexts[i].LocalContextData[WellKnownContextData.InternalId]!;
            }

            var results = await batchResolver(contexts, ids).ConfigureAwait(false);
            if (results.Count != contexts.Length)
            {
                throw ThrowHelper.BatchResolver_ResultCountMismatch(contexts.Length, results.Count);
            }

            for (var i = 0; i < contexts.Length; i++)
            {
                contexts[i].Result = results[i];
            }
        };
    }
}
