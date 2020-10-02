using System.Collections.Generic;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Data.Projections
{
    public class SelectionVisitor
    {
        /// <summary>
        /// The visitor default action.
        /// </summary>
        /// <value></value>
        protected virtual ISelectionVisitorAction DefaultAction { get; } = Continue;

        /// <summary>
        /// Ends traversing the graph.
        /// </summary>
        public static ISelectionVisitorAction Break { get; } = new BreakSelectionVisitorAction();

        /// <summary>
        /// Skips the child nodes and the current node.
        /// </summary>
        public static ISelectionVisitorAction Skip { get; } = new SkipSelectionVisitorAction();

        /// <summary>
        /// Continues traversing the graph.
        /// </summary>
        public static ISelectionVisitorAction Continue { get; } =
            new ContinueSelectionVisitorAction();

        /// <summary>
        /// Skips the child node but completes the current node.
        /// </summary>
        public static ISelectionVisitorAction SkipAndLeave { get; } =
            new SkipAndLeaveSelectionVisitorAction();
    }

    public class SelectionVisitor<TContext>
        : SelectionVisitor
        where TContext : ISelectionVisitorContext
    {
        protected virtual ISelectionVisitorAction Visit(
            IOutputField field,
            TContext context)
        {
            var result = Enter(field, context);

            if (result.Kind == SelectionVisitorActionKind.Continue)
            {
                if (VisitChildren(field, context).Kind == SelectionVisitorActionKind.Break)
                {
                    return Break;
                }
            }

            if (result.Kind == SelectionVisitorActionKind.Continue ||
                result.Kind == SelectionVisitorActionKind.SkipAndLeave)
            {
                result = Leave(field, context);
            }

            return result;
        }

        protected virtual ISelectionVisitorAction Visit(
            ISelection selection,
            TContext context)
        {
            var result = Enter(selection, context);

            if (result.Kind == SelectionVisitorActionKind.Continue)
            {
                if (VisitChildren(selection, context).Kind == SelectionVisitorActionKind.Break)
                {
                    return Break;
                }
            }

            if (result.Kind == SelectionVisitorActionKind.Continue ||
                result.Kind == SelectionVisitorActionKind.SkipAndLeave)
            {
                result = Leave(selection, context);
            }

            return result;
        }

        protected virtual ISelectionVisitorAction VisitChildren(
            IOutputField field,
            TContext context)
        {
            IOutputType type = field.Type;
            SelectionSetNode? selectionSet =
                context.SelectionSetNodes.Peek();

            ObjectType? objectType = type switch
            {
                ObjectType objType => objType,
                ListType listType => listType.InnerType() as ObjectType,
                _ => null
            };

            if (objectType is not null &&
                selectionSet is not null)
            {
                IReadOnlyList<IFieldSelection> selections = context.Context.GetSelections(
                    objectType,
                    selectionSet,
                    true);

                for (var i = 0; i < selections.Count; i++)
                {
                    if (selections[i] is ISelection selection)
                    {
                        if (Visit(selection, context).Kind == SelectionVisitorActionKind.Break)
                        {
                            return Break;
                        }
                    }
                }
            }

            return DefaultAction;
        }

        protected virtual ISelectionVisitorAction VisitChildren(
            ISelection selection,
            TContext context)
        {
            IObjectField field = selection.Field;
            return Visit(field, context);
        }

        protected virtual ISelectionVisitorAction Enter(
            IOutputField field,
            TContext context) =>
            DefaultAction;

        protected virtual ISelectionVisitorAction Leave(
            IOutputField field,
            TContext context) =>
            DefaultAction;

        protected virtual ISelectionVisitorAction Enter(
            ISelection selection,
            TContext context)
        {
            context.Selection.Push(selection);
            context.SelectionSetNodes.Push(selection.SelectionSet);
            return DefaultAction;
        }

        protected virtual ISelectionVisitorAction Leave(
            ISelection selection,
            TContext context)
        {
            context.Selection.Pop();
            context.SelectionSetNodes.Pop();
            return DefaultAction;
        }
    }

/*
    public class SelectionVisitorBaseOld<TContext>
        where TContext : ISelectionVisitorContext
    {
        protected virtual bool VisitSelections(
            IOutputType outputType,
            SelectionSetNode? selectionSet,
            TContext context)
        {
            (outputType, selectionSet) = UnwrapPaging(outputType, selectionSet);
            if (outputType.NamedType() is ObjectType type &&
                selectionSet is { })
            {
                foreach (IFieldSelection selection in CollectExtendedFields(type, selectionSet))
                {
                    if (EnterSelection(selection))
                    {
                        LeaveSelection(selection);
                    }
                }
            }
            else if (selectionSet is null)
            {
                throw new QueryException(
                    ErrorBuilder.New()
                        .SetMessage(
                            "UseSelection is in a invalid state. " +
                            "Selection set for type {0} was empty !",
                            outputType.NamedType().Name)
                        .Build());
            }
            else
            {
                throw new QueryException(
                    ErrorBuilder.New()
                        .SetMessage(
                            "UseSelection is in a invalid state. Type {0} " +
                            "is illegal!",
                            outputType.NamedType().Name)
                        .Build());
            }

            return true;
        }

        protected virtual void LeaveSelection(IFieldSelection selection)
        {
            Fields.Pop();
        }

        protected virtual bool EnterSelection(IFieldSelection selection)
        {
            Fields.Push(selection.Field);
            if (selection.Field.Type.IsLeafType() ||
                (selection.Field.Type.IsListType() &&
                    selection.Field.Type.ElementType().IsLeafType()))
            {
                if (EnterLeaf(selection))
                {
                    LeaveLeaf(selection);
                }
            }
            else if (selection.Field.Type.IsListType() ||
                selection.Field.Type.ToRuntimeType() == typeof(IConnection) ||
                (selection.Field.Member is PropertyInfo propertyInfo &&
                    ExtendedType.Tools.GetElementType(propertyInfo.PropertyType) is not null))
            {
                if (EnterList(selection))
                {
                    LeaveList(selection);
                }
            }
            else if (selection.Field.Type.IsObjectType())
            {
                if (EnterObject(selection))
                {
                    LeaveObject(selection);
                }
            }
            else
            {
                throw new QueryException(
                    ErrorBuilder.New()
                        .SetMessage(
                            string.Format(
                                "UseSelection is in a invalid state. Type {0} " +
                                "is illegal!",
                                selection.Field.Type.NamedType().Name))
                        .Build());
            }

            return true;
        }

        protected virtual bool EnterList(IFieldSelection selection)
        {
            (IOutputType type, SelectionSetNode? selectionSet) =
                UnwrapPaging(selection.Field.Type, selection.Selection.SelectionSet);
            return VisitSelections(type, selectionSet);
        }

        protected virtual void LeaveList(IFieldSelection selection)
        {
        }

        protected virtual bool EnterLeaf(IFieldSelection selection)
        {
            return true;
        }

        protected virtual void LeaveLeaf(IFieldSelection selection)
        {
        }

        protected virtual bool EnterObject(IFieldSelection selection)
        {
            return VisitSelections(selection.Field.Type, selection.Selection.SelectionSet);
        }

        protected virtual void LeaveObject(IFieldSelection selection)
        {
        }

        protected (IOutputType, IReadOnlyList<ISelection>?) UnwrapPaging(
            IOutputType outputType,
            IReadOnlyList<ISelection>? selections,
            TContext context)
        {
            if (outputType is IPageType connectionType &&
                selections is { })
            {
                if (TryUnwrapPaging(
                    context,
                    outputType,
                    selections,
                    out (IOutputType, IReadOnlyList<ISelection>) result))
                {
                    return result;
                }
                else
                {
                    if (connectionType.ItemType.InnerType() is IOutputType innerType)
                    {
                        return (innerType, ArraySegment<ISelection>.Empty);
                    }
                }
            }

            return (outputType, selections);
        }

        private bool TryUnwrapPaging(
            IOutputType outputType,
            IReadOnlyList<ISelection> selectionSet,
            TContext context,
            out (IOutputType, IReadOnlyList<ISelection>) result)
        {
            (IOutputType?, SelectionSetNode?) nullableResult = (null, null);

            if (outputType.ToRuntimeType() == typeof(IPage) &&
                outputType.NamedType() is ObjectType type)
            {
                foreach (IFieldSelection selection in context.Context.GetSelections(
                    outputType,
                    context.Fields.Peek().))
                {
                    IFieldSelection? currentSelection = GetPagingFieldOrDefault(selection);

                    if (currentSelection is not null)
                    {
                        nullableResult = MergeSelection(nullableResult.Item2, currentSelection);
                    }
                }
            }

            if (nullableResult.Item1 is not null && nullableResult.Item2 is not null)
            {
                result = (nullableResult.Item1, nullableResult.Item2);
                return true;
            }
            else
            {
                result = (outputType, selectionSet);
                return false;
            }
        }

        private IFieldSelection? GetPagingFieldOrDefault(IFieldSelection selection)
        {
            if (selection.Field.Name == "nodes")
            {
                return selection;
            }
            else if (selection.Field.Name == "edges" &&
                selection.Field.Type.NamedType() is ObjectType edgeType)
            {
                return Context
                    .GetSelections(edgeType, selection.Selection.SelectionSet)
                    .FirstOrDefault(x => x.Field.Name == "node");
            }

            return default;
        }

        private (IOutputType, SelectionSetNode?) MergeSelection(
            SelectionSetNode? selectionSet,
            IFieldSelection selection)
        {
            if (selectionSet is null)
            {
                selectionSet = selection.Selection.SelectionSet;
            }
            else if (selection.Selection.SelectionSet?.Selections is { })
            {
                selectionSet = selectionSet.WithSelections(
                    selectionSet.Selections.Concat(
                            selection.Selection.SelectionSet.Selections)
                        .ToList());
            }

            return (selection.Field.Type, selectionSet);
        }

        protected IReadOnlyList<IFieldSelection> CollectExtendedFields(
            ObjectType type,
            SelectionSetNode selectionSet)
        {
            IReadOnlyList<IFieldSelection> selections = Context.GetSelections(type, selectionSet);
            if (HasNonProjectableField(selections))
            {
                var fieldSelections = new List<ISelectionNode>();
                foreach (ObjectField field in type.Fields)
                {
                    if (field.Member is PropertyInfo && field.Type.IsLeafType())
                    {
                        fieldSelections.Add(CreateFieldNode(field.Name.Value));
                    }
                }

                selectionSet = selectionSet.AddSelections(fieldSelections.ToArray());
                selections = Context.GetSelections(type, selectionSet);
            }

            return selections;
        }

        private static bool HasNonProjectableField(IReadOnlyList<IFieldSelection> selections)
        {
            for (var i = 0; i < selections.Count; i++)
            {
                if (!(selections[i].Field.Member is PropertyInfo))
                {
                    return true;
                }
            }

            return false;
        }

        private static FieldNode CreateFieldNode(string fieldName) =>
            new FieldNode(
                null,
                new NameNode(fieldName),
                null,
                Array.Empty<DirectiveNode>(),
                Array.Empty<ArgumentNode>(),
                null);

    }
    */
}
