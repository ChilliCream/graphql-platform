#nullable enable

using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Internal;
using HotChocolate.Types;
using HotChocolate.Types.Attributes;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Resolvers.Expressions.Parameters;

internal sealed class IsSelectedParameterExpressionBuilder
    : IParameterExpressionBuilder
    , IParameterFieldConfiguration
    , IParameterBindingFactory
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

    public IParameterBinding Create(ParameterBindingContext context)
        => new IsSelectedBinding($"isSelected.{context.Parameter.Name}");

    public void ApplyConfiguration(ParameterInfo parameter, ObjectFieldDescriptor descriptor)
    {
        var attribute = parameter.GetCustomAttribute<IsSelectedAttribute>()!;

        if (attribute.Fields is not null)
        {
            var definition = descriptor.Extend().Configuration;
            definition.Tasks.Add(
                new OnCompleteTypeSystemConfigurationTask((ctx, def) =>
                    {
                        if (!ctx.DescriptorContext.ContextData.TryGetValue(WellKnownContextData.PatternValidationTasks,
                            out var value))
                        {
                            value = new List<IsSelectedPattern>();
                            ctx.DescriptorContext.ContextData[WellKnownContextData.PatternValidationTasks] = value;
                        }

                        var patterns = (List<IsSelectedPattern>)value!;
                        patterns.Add(new IsSelectedPattern((ObjectType)ctx.Type, def.Name, attribute.Fields));
                    },
                    definition,
                    ApplyConfigurationOn.AfterCompletion));

            descriptor.Use(
                next => async ctx =>
                {
                    var selectionContext = new IsSelectedContext(ctx.Schema, ctx.Select());
                    IsSelectedVisitor.Instance.Visit(attribute.Fields, selectionContext);
                    ctx.SetLocalState($"isSelected.{parameter.Name}", selectionContext.AllSelected);
                    await next(ctx);
                });
            return;
        }

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

    private class IsSelectedBinding(string key) : IParameterBinding
    {
        public ArgumentKind Kind => ArgumentKind.LocalState;

        public bool IsPure => false;

        public T Execute<T>(IResolverContext context)
            => context.GetLocalState<T>(key)!;
    }
}
