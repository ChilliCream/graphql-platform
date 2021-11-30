using System;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Internal;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static HotChocolate.Types.EntityFrameworkObjectFieldDescriptorExtensions;

namespace HotChocolate.Data.Internal;

internal sealed class ScopedDbConfigurationParameterExpressionBuilder<TDbContext>
    : IParameterExpressionBuilder
    , IParameterConfigurationBuilder
    where TDbContext : DbContext
{
    private readonly DbContextScope _scope;
    private readonly IParameterExpressionBuilder _innerBuilder =
        new CustomServiceParameterExpressionBuilder<TDbContext>();

    public ScopedDbConfigurationParameterExpressionBuilder(DbContextScope scope)
    {
        _scope = scope;
    }

    public ArgumentKind Kind => _innerBuilder.Kind;

    public bool IsPure => _innerBuilder.IsPure;

    public bool CanHandle(ParameterInfo parameter)
        => _innerBuilder.CanHandle(parameter);

    public Expression Build(ParameterInfo parameter, Expression context)
        => _innerBuilder.Build(parameter, context);

    public void ApplyConfiguration(ParameterInfo parameter, ObjectFieldDescriptor descriptor)
    {
        if (_scope is DbContextScope.Request)
        {
            descriptor.Extend().Definition.IsParallelExecutable = false;
        }
        else if (_scope is DbContextScope.Field)
        {
            descriptor.Extend().OnBeforeNaming(
                (_, d) =>
                {
                    FieldMiddlewareDefinition placeholder =
                        new(_ => _ => throw new NotSupportedException(), 
                            key: WellKnownMiddleware.ToList);

                    d.MiddlewareDefinitions.Insert(0, placeholder);

                    d.MiddlewareDefinitions.Insert(
                        0,
                        new(next => async context =>
                        {
                            IServiceProvider services = context.Services;
                            using IServiceScope fieldScope = services.CreateScope();

                            try
                            {
                                context.Services = fieldScope.ServiceProvider;
                                await next(context).ConfigureAwait(false);
                            }
                            finally
                            {
                                context.Services = services;
                            }
                        }));

                    AddCompletionMiddleware(d, placeholder);
                });
        }
        else
        {
            throw new NotSupportedException(
                $"DBContext scope {_scope} is not supported.");
        }
    }
}
