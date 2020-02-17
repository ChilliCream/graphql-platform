using System;
using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Resolvers;

namespace HotChocolate.Types.Selection
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

        protected virtual void VisitSelections(
            ObjectField field,
            SelectionSetNode selectionSet)
        {
            if (field.Type.NamedType() is ObjectType type)
            {
                foreach (IFieldSelection selection in Context.CollectFields(type, selectionSet))
                {
                    EnterSelection(selection);
                    LeaveSelection(selection);
                }
            }
            else
            {
                throw new Exception("Illegal type");
            }

        }
        protected virtual void LeaveSelection(
             IFieldSelection selection)
        {
            Fields.Pop();
        }

        protected virtual void EnterSelection(
             IFieldSelection selection)
        {
            Fields.Push(selection.Field);
            if (selection.Field.Type.IsListType())
            {
                EnterList(selection);
                LeaveList(selection);
            }
            else if (selection.Field.Type.IsLeafType())
            {
                EnterLeaf(selection);
                LeaveLeaf(selection);

            }
            else if (selection.Field.Type.IsObjectType())
            {
                EnterObject(selection);
                LeaveObject(selection);
            }
            else
            {
                throw new Exception("Illegal type");

            }
        }

        protected virtual void EnterList(IFieldSelection selection)
        {
            VisitSelections(selection.Field, selection.Selection.SelectionSet);
        }

        protected virtual void LeaveList(IFieldSelection selection)
        {

        }

        protected virtual void EnterLeaf(IFieldSelection selection)
        {

        }
        protected virtual void LeaveLeaf(IFieldSelection selection)
        {

        }

        protected virtual void EnterObject(IFieldSelection selection)
        {
            VisitSelections(selection.Field, selection.Selection.SelectionSet);
        }

        protected virtual void LeaveObject(IFieldSelection selection)
        {

        }

    }
}
