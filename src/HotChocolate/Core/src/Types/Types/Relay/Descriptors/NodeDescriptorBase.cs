#nullable enable

using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Configuration;
using HotChocolate.Properties;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Types.Helpers;
using HotChocolate.Utilities;
using static HotChocolate.Types.Relay.NodeResolverCompilerHelper;

#nullable enable

namespace HotChocolate.Types.Relay.Descriptors;

public abstract class NodeDescriptorBase : DescriptorBase<NodeDefinition>
{
    protected NodeDescriptorBase(IDescriptorContext context)
        : base(context)
    {
    }

    protected internal sealed override NodeDefinition Definition { get; protected set; } =
        new();

    protected abstract IObjectFieldDescriptor ConfigureNodeField();

    /// <summary>
    /// Specifies a delegate to resolve the node from its id.
    /// </summary>
    /// <param name="fieldResolver">
    /// The delegate to resolve the node from its id.
    /// </param>
    public virtual IObjectFieldDescriptor ResolveNode(
        FieldResolverDelegate fieldResolver)
    {
        Definition.ResolverField ??= new ObjectFieldDefinition();
        Definition.ResolverField.Resolver = fieldResolver ??
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
        if (fieldResolver is null)
        {
            throw new ArgumentNullException(nameof(fieldResolver));
        }

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
        if (method is null)
        {
            throw new ArgumentNullException(nameof(method));
        }

        var member = method.TryExtractMember();

        if (member is MethodInfo m)
        {
            Definition.ResolverField ??= new ObjectFieldDefinition();
            Definition.ResolverField.Member = m;
            Definition.ResolverField.ResolverType = typeof(TResolver);
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
        if (method is null)
        {
            throw new ArgumentNullException(nameof(method));
        }

        Definition.ResolverField ??= new ObjectFieldDefinition();
        Definition.ResolverField.Member = method;
        Definition.ResolverField.ResolverType = method.DeclaringType ?? typeof(object);
        return ConfigureNodeField();
    }

    protected void CompleteResolver(ITypeCompletionContext context, ObjectTypeDefinition definition)
    {
        var descriptorContext = context.DescriptorContext;

        if (Definition.ResolverField is not null)
        {
            // we let the descriptor complete on the definition object.
            ObjectFieldDescriptor
                .From(descriptorContext, Definition.ResolverField)
                .CreateDefinition();

            // after that all middleware should be available on the field definition and we can
            // start compiling the resolver and the resolver pipeline.
            if (Definition.ResolverField.Resolver is null &&
                Definition.ResolverField.Member is not null)
            {
                Definition.ResolverField.Resolvers =
                    Context.ResolverCompiler.CompileResolve(
                        Definition.ResolverField.Member,
                        typeof(object),
                        Definition.ResolverField.ResolverType,
                        parameterExpressionBuilders: ParameterExpressionBuilders);
            }

            if (Definition.ResolverField.Resolver is not null)
            {
                var pipeline = FieldMiddlewareCompiler.Compile(
                    context.GlobalComponents,
                    Definition.ResolverField.GetMiddlewareDefinitions(),
                    Definition.ResolverField.GetResultConverters(),
                    Definition.ResolverField.Resolver,
                    false);

                definition.ContextData[WellKnownContextData.NodeResolver] =
                    new NodeResolverInfo(null, pipeline!);
            }
        }
    }

    protected static class ConverterHelper
    {
        private static ResultFormatterDefinition? _resultConverter;

        private static ResultFormatterDefinition Formatter
        {
            get => _resultConverter ??= IdMiddleware.Create();
        }

        public static IObjectFieldDescriptor TryAdd(IObjectFieldDescriptor descriptor)
        {
            var converters = descriptor.Extend().Definition.FormatterDefinitions;

            if (!converters.Contains(Formatter))
            {
                converters.Add(Formatter);
            }

            return descriptor;
        }
    }
}
