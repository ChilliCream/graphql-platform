#nullable enable

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Internal;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

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
}

internal sealed class IsSelectedVisitor : SyntaxWalker<IsSelectedContext>
{
    protected override ISyntaxVisitorAction Enter(FieldNode node, IsSelectedContext context)
    {
        var selections = context.Selections.Peek();
        var typeContext = context.TypeContext.Peek();

        if (!selections.IsSelected(node.Alias?.Value ?? node.Name.Value))
        {
            context.AllSelected = false;
            return Break;
        }


        context.TypeContext.Push(null);
        // context.Selections.Push();

        return base.Enter(node, context);
    }

    protected override ISyntaxVisitorAction Leave(FieldNode node, IsSelectedContext context)
    {
        if (context.AllSelected)
        {
            context.TypeContext.Pop();
            context.Selections.Pop();
        }

        return base.Leave(node, context);
    }

    protected override ISyntaxVisitorAction Enter(InlineFragmentNode node, IsSelectedContext context)
    {
        context.TypeContext.Push(
            node.TypeCondition is not null
                ? context.Schema.GetType<INamedType>(node.TypeCondition.Name.Value)
                : null);
        return base.Enter(node, context);
    }


    protected override ISyntaxVisitorAction Leave(InlineFragmentNode node, IsSelectedContext context)
    {
        context.TypeContext.Pop();
        return base.Leave(node, context);
    }

    public static IsSelectedVisitor Instance { get; } = new();
}

internal sealed class IsSelectedContext
{
    public IsSelectedContext(ISchema schema, ISelectionCollection selections)
    {
        Schema = schema;
        Selections.Push(selections);
        TypeContext.Push(null);
    }

    public ISchema Schema { get; }

    public Stack<ISelectionCollection> Selections { get; } = new();

    public Stack<INamedType?> TypeContext { get; } = new();

    public bool AllSelected { get; set; } = true;
}

internal sealed class ValidateIsSelectedPatternVisitor : SyntaxWalker<ValidateIsSelectedPatternContext>
{
    protected override ISyntaxVisitorAction Enter(FieldNode node, ValidateIsSelectedPatternContext context)
    {
        var field = context.Field.Peek();
        var typeContext = context.TypeContext.Peek() ?? field.Type.NamedType();

        if (typeContext is IComplexOutputType complexOutputType)
        {
            if (complexOutputType.Fields.TryGetField(node.Name.Value, out var objectField))
            {
                context.TypeContext.Push(null);
                context.Field.Push(objectField);
            }
            else
            {
                context.Error = SchemaErrorBuilder.New().SetMessage("Broken").Build();
                return Break;
            }
        }
        else
        {
            context.Error = SchemaErrorBuilder.New().SetMessage("Broken").Build();
            return Break;
        }

        return base.Enter(node, context);
    }

    protected override ISyntaxVisitorAction Leave(FieldNode node, ValidateIsSelectedPatternContext context)
    {
        context.TypeContext.Pop();
        return base.Leave(node, context);
    }

    protected override ISyntaxVisitorAction Enter(InlineFragmentNode node, ValidateIsSelectedPatternContext context)
    {
        if (node.TypeCondition is not null)
        {
            var type = context.Schema.GetType<INamedType>(node.TypeCondition.Name.Value);
            var field = context.Field.Peek();

            if (!type.IsAssignableFrom(field.Type.NamedType()))
            {
                context.Error = SchemaErrorBuilder.New().SetMessage("Broken").Build();
                return Break;
            }

            context.TypeContext.Push(type);
        }

        return base.Enter(node, context);
    }


    protected override ISyntaxVisitorAction Leave(InlineFragmentNode node, ValidateIsSelectedPatternContext context)
    {
        if (node.TypeCondition is not null)
        {
            context.TypeContext.Pop();
        }

        return base.Leave(node, context);
    }

    public static ValidateIsSelectedPatternVisitor Instance { get; } = new();
}

internal sealed class ValidateIsSelectedPatternContext
{
    public ValidateIsSelectedPatternContext(ISchema schema, IObjectField field)
    {
        Schema = schema;
        Field.Push(field);
        TypeContext.Push(null);
    }

    public ISchema Schema { get; }

    public Stack<IOutputField> Field { get; } = new();

    public Stack<INamedType?> TypeContext { get; } = new();

    public ISchemaError? Error { get; set; }
}

internal sealed class IsSelectedPattern(ObjectType type, string fieldName, SelectionSetNode pattern)
{
    public ObjectType Type { get; } = type;
    public string FieldName { get; } = fieldName;
    public SelectionSetNode Pattern { get; } = pattern;
}
