using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Internal;
using HotChocolate.Resolvers.Expressions.Parameters;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using Microsoft.EntityFrameworkCore;
using static HotChocolate.Types.EntityFrameworkObjectFieldDescriptorExtensions;
using static HotChocolate.WellKnownMiddleware;

namespace HotChocolate.Data;

internal sealed class DbContextParameterExpressionBuilder<TDbContext>
    : IParameterExpressionBuilder
    , IParameterFieldConfiguration
    where TDbContext : DbContext
{
    private readonly ServiceKind _kind;

    public DbContextParameterExpressionBuilder(DbContextKind kind)
    {
        _kind = kind switch
        {
            DbContextKind.Pooled => ServiceKind.Pooled,
            DbContextKind.Resolver => ServiceKind.Resolver,
            _ => ServiceKind.Synchronized
        };
    }

    public ArgumentKind Kind => ArgumentKind.Service;

    public bool IsPure => false;

    public bool IsDefaultHandler => false;

    public bool CanHandle(ParameterInfo parameter)
        => parameter.ParameterType == typeof(TDbContext);

    public void ApplyConfiguration(ParameterInfo parameter, ObjectFieldDescriptor descriptor)
    {
        switch (_kind)
        {
            case ServiceKind.Pooled:
                UseDbContext<TDbContext>(descriptor.Extend().Definition);
                break;

            case ServiceKind.Synchronized:
                ServiceExpressionHelper.ApplyConfiguration(parameter, descriptor, _kind);
                break;

            case ServiceKind.Resolver:
                ServiceExpressionHelper.ApplyConfiguration(parameter, descriptor, _kind);
                var definition = descriptor.Extend().Definition;
                var placeholderMiddleware = new FieldMiddlewareDefinition(
                    _ => _ => throw new NotSupportedException(),
                    key: ToList);
                var serviceMiddleware =
                    definition.MiddlewareDefinitions.Last(t => t.Key == ResolverService);
                var index = definition.MiddlewareDefinitions.IndexOf(serviceMiddleware) + 1;
                definition.MiddlewareDefinitions.Insert(index, placeholderMiddleware);
                AddCompletionMiddleware(definition, placeholderMiddleware);
                break;
        }
    }

    public Expression Build(ParameterInfo parameter, Expression context)
        => ServiceExpressionHelper.Build(parameter, context, _kind);
}
