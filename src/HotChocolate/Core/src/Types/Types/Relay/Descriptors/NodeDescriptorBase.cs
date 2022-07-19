using System;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Properties;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
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

    public virtual IObjectFieldDescriptor ResolveNode(
        FieldResolverDelegate fieldResolver)
    {
        Definition.Resolver = fieldResolver ??
            throw new ArgumentNullException(nameof(fieldResolver));

        return ConfigureNodeField();
    }

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
            var resolver =
                Context.ResolverCompiler.CompileResolve(
                    m,
                    typeof(object),
                    typeof(TResolver),
                    ParameterExpressionBuilders);
            return ResolveNode(resolver.Resolver!);
        }

        throw new ArgumentException(
            TypeResources.NodeDescriptor_MustBeMethod,
            nameof(member));
    }

    public IObjectFieldDescriptor ResolveNodeWith(MethodInfo method)
    {
        if (method is null)
        {
            throw new ArgumentNullException(nameof(method));
        }

        var resolver =
            Context.ResolverCompiler.CompileResolve(
                method,
                typeof(object),
                method.DeclaringType ?? typeof(object),
                ParameterExpressionBuilders);

        return ResolveNode(resolver.Resolver!);
    }

    protected static class ConverterHelper
    {
        private static ResultConverterDefinition? _resultConverter;

        private static ResultConverterDefinition Converter
        {
            get => _resultConverter ??= IdMiddleware.Create();
        }

        public static IObjectFieldDescriptor TryAdd(IObjectFieldDescriptor descriptor)
        {
            var converters =
                descriptor.Extend().Definition.ResultConverters;

            if (!converters.Contains(Converter))
            {
                converters.Add(Converter);
            }

            return descriptor;
        }
    }
}
