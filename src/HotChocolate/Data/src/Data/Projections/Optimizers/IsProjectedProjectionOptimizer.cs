using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Execution.Processing;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using static HotChocolate.Data.Projections.ProjectionConvention;

namespace HotChocolate.Data.Projections.Handlers;

public class IsProjectedProjectionOptimizer : IProjectionOptimizer
{
    private const string _aliasPrefix = "__projection_alias_";

    private static readonly string[] _fieldAliasesCache = Enumerable.Range(0, 8)
        .Select(i => _aliasPrefix + i)
        .ToArray();

    private static string GetAlias(int i)
    {
        if (i < _fieldAliasesCache.Length)
            return _fieldAliasesCache[i];
        return _aliasPrefix + i;
    }

    public Selection RewriteSelection(
        SelectionSetOptimizerContext context,
        Selection selection)
    {
        if (context.Type is not ObjectType objectType ||
            !objectType.ContextData.TryGet(AlwaysProjectedFieldsKey, out var alwaysProjectedFieldNames))
        {
            return selection;
        }

        for (int aliasedFieldIndex = 0; aliasedFieldIndex < alwaysProjectedFieldNames.Length; aliasedFieldIndex++)
        {
            var alias = GetAlias(aliasedFieldIndex);
            var fieldName = alwaysProjectedFieldNames[aliasedFieldIndex];

            if (context.IsFieldAlreadyInSelection(fieldName, alias))
                continue;

            context.AddNewFieldToSelection(selection, objectType, fieldName, alias);
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
    // public readonly Type Type => (Type?) Values[^1] ?? throw new InvalidOperationException();
    public readonly string TypeName => (string?) Values[^1] ?? throw new InvalidOperationException();

    public static ProjectedValue? FromObject(object value)
    {
        if (value is object[] { Length: > 0 } values)
            return new ProjectedValue(values);
        return null;
    }

    // Encapsulate what we store in the object array here.
    public static IEnumerable<Expression> AppendObjectType(
        IEnumerable<Expression> initializers, ObjectType objectType)
    {
        var initializersWithType = initializers
            .Append(Expression.Constant(objectType.Name));
        return initializersWithType/*.ToArray()*/;
    }
}

public static class ContextExtensions
{
    public static object GetRawParent(this IPureResolverContext context)
    {
        return context.Parent<object>();
    }

    public static ProjectedValue GetProjectedParent(this IPureResolverContext context)
    {
        var originalParent = context.GetRawParent();
        return new ProjectedValue((object?[]) originalParent);
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

    public Selection RewriteSelection(
        SelectionSetOptimizerContext context,
        Selection selection)
    {
        if (selection.Field.Member is not PropertyInfo)
        {
            return selection;
        }

        var runtimeType = context.Type.RuntimeType;

        if (!Temp1.Factories.TryGetValue(context.Path, out var converter))
        {
            // TODO: what does an "object" mean here?
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
            var s = selectionEnumerator.Current;
            var selectionNode = s.SyntaxNode;

            string fieldName = selectionNode.Alias?.Value ?? selectionNode.Name.Value;

            if (selection.ResponseName == fieldName)
            {
                var namedType = selection.Field.Type.NamedType();

                if (namedType.IsAbstractType())
                {
                    Temp.OfType.Add(selection.Id,
                        (o, type) =>
                        {
                            if (ProjectedValue.FromObject(o) is { } projectedValue)
                                return projectedValue.TypeName.Equals(type.Name);
                            return false;
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
