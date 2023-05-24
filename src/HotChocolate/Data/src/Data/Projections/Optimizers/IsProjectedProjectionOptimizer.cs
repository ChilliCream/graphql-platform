using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Utilities.Serialization;
using static HotChocolate.Data.Projections.ProjectionConvention;

namespace HotChocolate.Data.Projections.Handlers;

public class IsProjectedProjectionOptimizer : IProjectionOptimizer
{
    public bool CanHandle(ISelection field) =>
        field.DeclaringType is ObjectType objectType &&
        objectType.ContextData.ContainsKey(AlwaysProjectedFieldsKey);

    public Selection RewriteSelection(
        SelectionSetOptimizerContext context,
        Selection selection)
    {
        if (!(context.Type is ObjectType type &&
                type.ContextData.TryGetValue(AlwaysProjectedFieldsKey, out var fieldsObj) &&
                fieldsObj is string[] fields))
        {
            return selection;
        }

        for (var i = 0; i < fields.Length; i++)
        {
            var alias = "__projection_alias_" + i;

            // if the field is already in the selection set we do not need to project it
            if (context.Selections.TryGetValue(fields[i], out var field) &&
                field.Field.Name == fields[i])
            {
                continue;
            }

            // if the field is already added as an alias we do not need to add it
            if (context.Selections.TryGetValue(alias, out field) &&
                field.Field.Name == fields[i])
            {
                continue;
            }

            IObjectField nodesField = type.Fields[fields[i]];
            var nodesFieldNode = new FieldNode(
                null,
                new NameNode(fields[i]),
                new NameNode(alias),
                null,
                Array.Empty<DirectiveNode>(),
                Array.Empty<ArgumentNode>(),
                null);

            var nodesPipeline = context.CompileResolverPipeline(nodesField, nodesFieldNode);

            var compiledSelection = new Selection.Sealed(
                context.GetNextSelectionId(),
                context.Type,
                nodesField,
                nodesField.Type,
                nodesFieldNode,
                alias,
                resolverPipeline: nodesPipeline,
                arguments: selection.Arguments,
                isInternal: true);

            context.AddSelection(compiledSelection);
        }

        return selection;
    }
}

public struct ProjectedValue
{
    public ProjectedValue(object?[] values)
    {
        Values = values;
    }

    public object?[] Values { get; }
    public readonly object? this[int index] => Values[index];
    public readonly Type Type => (Type?) Values[^1] ?? throw new InvalidOperationException();
}

public static class ContextExtensions
{
    public static ProjectedValue GetProjectedParent(this IPureResolverContext context)
    {
        return new ProjectedValue(context.Parent<object[]>());
    }
}

public class RewriteToIndexerOptimizer : IProjectionOptimizer
{
    private static class Temp1
    {
        public static Dictionary<SelectionPath, Func<object[], object>> Factories
        {
            get;
        } = new();
    }

    public bool CanHandle(ISelection field) => field.DeclaringType is ObjectType;

    public Selection RewriteSelection(
        SelectionSetOptimizerContext context,
        Selection selection)
    {
        var runtimeType = context.Type.RuntimeType;

        if (!Temp1.Factories.TryGetValue(context.Path, out var converter))
        {
            if (runtimeType == typeof(object))
            {
                converter = o =>
                {
                    // maybe dict
                    return new object();
                };
            }
            else
            {
                var memberInfos =
                    context.Selections.Select(x => x.Value.Field.Member).ToArray();

                converter = o =>
                {
                    var value = Activator.CreateInstance(runtimeType);
                    for (var i = 0; i < memberInfos.Length; i++)
                    {
                        if (memberInfos[i] is PropertyInfo { CanWrite: true } p)
                        {
                            p.SetValue(value, o[i]);
                        }
                    }

                    return value!;
                };
            }

            Temp1.Factories.Add(context.Path, converter);
        }

        using var selectionEnumerator = context.Selections.Values.GetEnumerator();
        int i = 0;
        while (selectionEnumerator.MoveNext())
        {
            var fn = selectionEnumerator.Current.SyntaxNode;

            if (selection.ResponseName == (fn.Alias?.Value ?? fn.Name.Value))
            {
                // abstract type resolver
                if (selection.Field.Type.NamedType().IsAbstractType())
                {
                    Temp.OfType.Add(selection.Id,
                        (o, type) =>
                        {
                            if (o is not object[] values || values.Length < 1 )
                            {
                                return false;
                            }

                            return values[^1].Equals(type.Name);
                        });
                }
                Temp.ValueConverter[selection.Id] = converter;
                // TODO check if this really works
                var index = i;
                if (selection.Field.Member is PropertyInfo)
                {
                    if (selection.Strategy == SelectionExecutionStrategy.Pure)
                    {
                        return new Selection(
                            selection.Id,
                            selection.DeclaringType,
                            selection.Field,
                            selection.Type,
                            selection.SyntaxNode,
                            selection.ResponseName,
                            pureResolver: c =>
                            {
                                var parent = c.GetProjectedParent();
                                return parent[index];
                            },
                            arguments: selection.Arguments,
                            isInternal: false);
                    }
                    else
                    {
                        FieldDelegate resolverPipeline =
                            selection.ResolverPipeline ??
                            context.CompileResolverPipeline(selection.Field, selection.SyntaxNode);

                        FieldDelegate WrappedPipeline(FieldDelegate next) =>
                            ctx =>
                            {
                                var parent = ctx.GetProjectedParent();
                                ctx.Result = parent[index];
                                return next(ctx);
                            };

                        resolverPipeline = WrappedPipeline(resolverPipeline);

                        return new Selection(
                            selection.Id,
                            selection.DeclaringType,
                            selection.Field,
                            selection.Type,
                            selection.SyntaxNode,
                            selection.ResponseName,
                            arguments: selection.Arguments,
                            resolverPipeline: resolverPipeline,
                            isInternal: false);
                    }
                }
            }

            i++;
        }

        return selection;
    }
}
