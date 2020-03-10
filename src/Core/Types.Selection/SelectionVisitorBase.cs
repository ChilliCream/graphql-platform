using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types.Relay;

namespace HotChocolate.Types.Selections
{
    public class SelectionVisitorBase
    {
        protected SelectionVisitorBase(
            IResolverContext context)
        {
            Context = context
                ?? throw new ArgumentNullException(nameof(context));
        }

        protected Stack<ObjectField> Fields { get; } =
            new Stack<ObjectField>();

        protected readonly IResolverContext Context;

        protected virtual bool VisitSelections(
            IOutputType outputType,
            SelectionSetNode selectionSet)
        {
            (outputType, selectionSet) = UnwrapPaging(outputType, selectionSet);
            if (outputType.NamedType() is ObjectType type)
            {
                foreach (IFieldSelection selection in Context.CollectFields(type, selectionSet))
                {
                    if (EnterSelection(selection))
                    {
                        LeaveSelection(selection);
                    }
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
                                outputType.NamedType().Name))
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
            if (selection.Field.Type.IsListType() ||
                selection.Field.Type.ToClrType() == typeof(IConnection))
            {
                if (EnterList(selection))
                {
                    LeaveList(selection);
                }
            }
            else if (selection.Field.Type.IsLeafType())
            {
                if (EnterLeaf(selection))
                {
                    LeaveLeaf(selection);
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
            (IOutputType type, SelectionSetNode selectionSet) =
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

        protected (IOutputType, SelectionSetNode) UnwrapPaging(
            IOutputType outputType,
            SelectionSetNode selectionSet)
        {
            if (TryUnwrapPaging(
                outputType,
                selectionSet,
                out (IOutputType, SelectionSetNode) result))
            {
                return result;
            }
            return (outputType, selectionSet);
        }

        private bool TryUnwrapPaging(
            IOutputType outputType,
            SelectionSetNode selectionSet,
            out (IOutputType, SelectionSetNode) result)
        {
            result = (null, null);

            if (outputType.ToClrType() == typeof(IConnection) &&
               outputType.NamedType() is ObjectType type)
            {
                foreach (IFieldSelection selection in Context.CollectFields(type, selectionSet))
                {
                    IFieldSelection currentSelection = GetPagingFieldOrDefault(selection);

                    if (currentSelection != null)
                    {
                        result = MergeSelection(result.Item2, currentSelection);
                    }
                }
            }
            return result.Item2 != null;
        }

        private IFieldSelection GetPagingFieldOrDefault(IFieldSelection selection)
        {
            if (selection.Field.Name == "nodes")
            {
                return selection;
            }
            else if (selection.Field.Name == "edges" &&
                selection.Field.Type.NamedType() is ObjectType edgeType)
            {
                return Context
                    .CollectFields(edgeType, selection.Selection.SelectionSet)
                    .FirstOrDefault(x => x.Field.Name == "node");
            }
            return null;
        }

        private (IOutputType, SelectionSetNode) MergeSelection(
            SelectionSetNode selectionSet,
            IFieldSelection selection)
        {
            if (selectionSet == null)
            {
                selectionSet = selection.Selection.SelectionSet;
            }
            else
            {
                selectionSet = selectionSet.WithSelections(
                    selectionSet.Selections.Concat(
                        selection.Selection.SelectionSet.Selections)
                    .ToList());
            }
            return (selection.Field.Type, selectionSet);
        }
    }
}
