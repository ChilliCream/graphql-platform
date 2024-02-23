using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Internal;
using HotChocolate.Resolvers.Expressions.Parameters;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using Microsoft.EntityFrameworkCore;

namespace HotChocolate.Data;

internal sealed class InferDbContextParameterExpressionBuilder()
    : IParameterExpressionBuilder
    , IParameterFieldConfiguration
{
    public ArgumentKind Kind => ArgumentKind.Service;

    public bool IsPure => false;

    public bool IsDefaultHandler => false;

    public bool CanHandle(ParameterInfo parameter)
        => typeof(DbContext).IsAssignableFrom(parameter.ParameterType);

    public void ApplyConfiguration(ParameterInfo parameter, ObjectFieldDescriptor descriptor)
    {
        ServiceExpressionHelper.ApplyConfiguration(parameter, descriptor, ServiceKind.Resolver);
        var definition = descriptor.Extend().Definition;
        var placeholderMiddleware = new FieldMiddlewareDefinition(
            _ => _ => throw new NotSupportedException(),
            key: WellKnownMiddleware.ToList);
        var serviceMiddleware =
            definition.MiddlewareDefinitions.Last(t => t.Key == WellKnownMiddleware.ResolverService);
        var index = definition.MiddlewareDefinitions.IndexOf(serviceMiddleware) + 1;
        definition.MiddlewareDefinitions.Insert(index, placeholderMiddleware);
        EntityFrameworkObjectFieldDescriptorExtensions.AddCompletionMiddleware(definition, placeholderMiddleware);
    }

    public Expression Build(ParameterExpressionBuilderContext context)
        => ServiceExpressionHelper.Build(context.Parameter, context.ResolverContext, ServiceKind.Resolver);
}
