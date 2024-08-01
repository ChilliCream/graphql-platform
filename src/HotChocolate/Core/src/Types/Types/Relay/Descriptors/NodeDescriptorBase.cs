#nullable enable

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

namespace HotChocolate.Types.Relay.Descriptors;

public abstract class NodeDescriptorBase(IDescriptorContext context)
    : DescriptorBase<NodeDefinition>(context)
{
    protected internal sealed override NodeDefinition Definition { get; protected set; } = new();

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

                var directiveDefs = Definition.ResolverField.GetDirectives();

                if (directiveDefs.Count > 0)
                {
                    var directives =
                        DirectiveCollection.CreateAndComplete(
                            context,
                            DirectiveLocation.FieldDefinition,
                            Definition.ResolverField,
                            directiveDefs);

                    foreach (var directive in directives)
                    {
                        if (directive.Type.Middleware is not null)
                        {
                            pipeline = directive.Type.Middleware.Invoke(pipeline, directive);
                        }
                    }
                }

                definition.ContextData[WellKnownContextData.NodeResolver] =
                    new NodeResolverInfo(null, pipeline!);
            }
        }
    }

    protected static class ConverterHelper
    {
        public static IObjectFieldDescriptor TryAdd(IObjectFieldDescriptor descriptor)
        {
            var extensions = descriptor.Extend();
            var context = extensions.Context;

            if (!context.ContextData.TryGetValue(WellKnownContextData.NodeIdResultFormatter, out var value) ||
                value is null)
            {
                value = Create(context.NodeIdSerializerAccessor);
                context.ContextData[WellKnownContextData.NodeIdResultFormatter] = value;
            }

            var formatter = (ResultFormatterDefinition)value;
            var converters = extensions.Definition.FormatterDefinitions;

            if (!converters.Contains(formatter))
            {
                converters.Add(formatter);
            }

            return descriptor;
        }

        public static ResultFormatterDefinition Create(
            INodeIdSerializerAccessor serializerAccessor)
            => new((context, result)
                    => result is not null
                        ? serializerAccessor.Serializer.Format(context.ObjectType.Name, result)
                        : null,
                key: WellKnownMiddleware.GlobalId,
                isRepeatable: false);
    }
}
