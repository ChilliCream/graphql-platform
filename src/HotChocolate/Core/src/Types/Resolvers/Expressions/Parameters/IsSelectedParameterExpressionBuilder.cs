#nullable enable
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Internal;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Resolvers.Expressions.Parameters;

internal sealed class IsSelectedParameterExpressionBuilder : IParameterExpressionBuilder, IParameterFieldConfiguration
{
    public ArgumentKind Kind => ArgumentKind.LocalState;

    public bool IsPure => false;

    public bool IsDefaultHandler => false;

    public bool CanHandle(ParameterInfo parameter)
        => parameter.IsDefined(typeof(IsSelectedAttribute));

    public Expression Build(ParameterExpressionBuilderContext context)
    {
        var parameter = context.Parameter;
        var key = $"isSelected.{parameter.Name}";
        Expression<Func<IResolverContext, bool>> expr = ctx => ctx.GetLocalState<bool>(key);
        return Expression.Invoke(expr, context.ResolverContext);
    }

    public void ApplyConfiguration(ParameterInfo parameter, ObjectFieldDescriptor descriptor)
    {
        var attribute = parameter.GetCustomAttribute<IsSelectedAttribute>()!;
        
        switch (attribute.FieldNames.Length)
        {
            case 1:
            {
                var fieldName = attribute.FieldNames[0];

                descriptor.Use(
                    next => async ctx =>
                    {
                        var isSelected = ctx.IsSelected(fieldName);
                        ctx.SetLocalState($"{nameof(isSelected)}.{parameter.Name}", isSelected);
                        await next(ctx);
                    });
                break;
            }

            case 2:
            {
                var fieldName1 = attribute.FieldNames[0];
                var fieldName2 = attribute.FieldNames[1];

                descriptor.Use(
                    next => async ctx =>
                    {
                        var isSelected = ctx.IsSelected(fieldName1, fieldName2);
                        ctx.SetLocalState($"{nameof(isSelected)}.{parameter.Name}", isSelected);
                        await next(ctx);
                    });
                break;
            }

            case 3:
            {
                var fieldName1 = attribute.FieldNames[0];
                var fieldName2 = attribute.FieldNames[1];
                var fieldName3 = attribute.FieldNames[2];

                descriptor.Use(
                    next => async ctx =>
                    {
                        var isSelected = ctx.IsSelected(fieldName1, fieldName2, fieldName3);
                        ctx.SetLocalState($"{nameof(isSelected)}.{parameter.Name}", isSelected);
                        await next(ctx);
                    });
                break;
            }

            case > 3:
            {
                var fieldNames = new HashSet<string>(attribute.FieldNames);

                descriptor.Use(
                    next => async ctx =>
                    {
                        var isSelected = ctx.IsSelected(fieldNames);
                        ctx.SetLocalState($"{nameof(isSelected)}.{parameter.Name}", isSelected);
                        await next(ctx);
                    });
                break;
            }
        }
    }
}