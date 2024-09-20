#nullable enable

using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Internal;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
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
            var definition = descriptor.Extend().Definition;
            definition.Configurations.Add(
                new CompleteConfiguration((ctx, def) =>
                {
                    if(!ctx.DescriptorContext.ContextData.TryGetValue(WellKnownContextData.PatternValidationTasks, out var value))
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

    private sealed class IsSelectedVisitor : SyntaxWalker<IsSelectedContext>
    {
        protected override ISyntaxVisitorAction Enter(FieldNode node, IsSelectedContext context)
        {
            var selections = context.Selections.Peek();
            var responseName = node.Alias?.Value ?? node.Name.Value;

            if (!selections.IsSelected(responseName))
            {
                context.AllSelected = false;
                return Break;
            }

            if (node.SelectionSet is not null)
            {
                context.Selections.Push(selections.Select(responseName));
            }

            return base.Enter(node, context);
        }

        protected override ISyntaxVisitorAction Leave(FieldNode node, IsSelectedContext context)
        {
            if (node.SelectionSet is not null)
            {
                context.Selections.Pop();
            }

            return base.Leave(node, context);
        }

        protected override ISyntaxVisitorAction Enter(InlineFragmentNode node, IsSelectedContext context)
        {
            if (node.TypeCondition is not null)
            {
                var typeContext = context.Schema.GetType<INamedType>(node.TypeCondition.Name.Value);
                var selections = context.Selections.Peek();
                context.Selections.Push(selections.Select(typeContext));
            }

            return base.Enter(node, context);
        }

        protected override ISyntaxVisitorAction Leave(InlineFragmentNode node, IsSelectedContext context)
        {
            if (node.TypeCondition is not null)
            {
                context.Selections.Pop();
            }

            return base.Leave(node, context);
        }

        public static IsSelectedVisitor Instance { get; } = new();
    }

    private sealed class IsSelectedContext
    {
        public IsSelectedContext(ISchema schema, ISelectionCollection selections)
        {
            Schema = schema;
            Selections.Push(selections);
        }

        public ISchema Schema { get; }

        public Stack<ISelectionCollection> Selections { get; } = new();

        public bool AllSelected { get; set; } = true;
    }

    private class IsSelectedBinding(string key) : IParameterBinding
    {
        public ArgumentKind Kind => ArgumentKind.LocalState;

        public bool IsPure => false;

        public T Execute<T>(IResolverContext context)
            => context.GetLocalState<T>(key)!;
    }
}
