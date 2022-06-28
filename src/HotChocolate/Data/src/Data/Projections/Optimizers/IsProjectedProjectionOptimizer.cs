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

public class RewriteToIndexerOptimizer : IProjectionOptimizer
{
    public bool CanHandle(ISelection field) => field.DeclaringType is ObjectType;

    public Selection RewriteSelection(
        SelectionOptimizerContext context,
        Selection selection)
    {
        if (!Temp.Factories.TryGetValue(context.Path, out var converter))
        {
            var runtimeType = context.Type.RuntimeType;
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
                MemberInfo?[] memberInfos =
                    context.Fields.Select(x => x.Value.Field.Member).ToArray();

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

            Temp.Factories.Add(context.Path, converter);
        }
        for (var i = 0; i < context.SelectionSet.Selections.Count; i++)
        {
            ISelectionNode selectionNode = context.SelectionSet.Selections[i];

            if (selectionNode is FieldNode fn &&
                selection.ResponseName == (fn.Alias?.Value ?? fn.Name.Value))
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

                            return values[values.Length - 1].Equals(type.Name.Value);
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
                            selection.SyntaxNode,
                            null,
                            c => c.Parent<object[]>()[index],
                            arguments: selection.Arguments,
                            internalSelection: false);
                    }
                    else
                    {
                        FieldDelegate resolverPipeline =
                            selection.ResolverPipeline ??
                            context.CompileResolverPipeline(selection.Field, selection.SyntaxNode);

                        FieldDelegate WrappedPipeline(FieldDelegate next) =>
                            ctx =>
                            {
                                ctx.Result = ctx.Parent<object[]>()[index];
                                return next(ctx);
                            };

                        resolverPipeline = WrappedPipeline(resolverPipeline);

                        return new Selection(
                            selection.Id,
                            selection.DeclaringType,
                            selection.Field,
                            selection.SyntaxNode,
                            resolverPipeline,
                            arguments: selection.Arguments,
                            internalSelection: false);
                    }
                }
            }
        }

        return selection;
    }
}
