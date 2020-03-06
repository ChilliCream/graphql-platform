using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Selections
{
    public class SelectionVisitor
        : SelectionVisitorBase
    {
        private readonly ITypeConversion _converter;
        private readonly IReadOnlyList<IListHandler> _listHandler = ListHandlers.All;

        public SelectionVisitor(
            IResolverContext context,
            ITypeConversion converter)
            : base(context)
        {
            _converter = converter;
        }

        protected Stack<SelectionClosure> Closures { get; } =
            new Stack<SelectionClosure>();

        public void Accept(ObjectField field)
        {
            IOutputType type = field.Type;
            SelectionSetNode selectionSet = Context.FieldSelection.SelectionSet;
            (type, selectionSet) = UnwrapPaging(type, selectionSet);
            IType elementType = type.IsListType() ? type.ElementType() : type;
            Closures.Push(new SelectionClosure(elementType.ToClrType(), "e"));
            VisitSelections(type, selectionSet);
        }

        public Expression<Func<T, T>> Project<T>()
        {
            return (Expression<Func<T, T>>)Closures.Peek().CreateMemberInitLambda();
        }

        protected override void LeaveLeaf(IFieldSelection selection)
        {
            if (selection.Field.Member is PropertyInfo member)
            {
                SelectionClosure closure = Closures.Peek();

                closure.Projections[member.Name] = Expression.Bind(
                    member, Expression.Property(closure.Instance.Peek(), member));
            }
            base.LeaveLeaf(selection);
        }

        protected override void LeaveObject(IFieldSelection selection)
        {
            if (selection.Field.Member is PropertyInfo)
            {
                Expression memberInit = Closures.Pop().CreateMemberInit();

                Closures.Peek().Projections[selection.Field.Name] =
                    Expression.Bind(selection.Field.Member, memberInit);

                base.LeaveObject(selection);
            }
            else
            {
                throw new QueryException(
                    ErrorBuilder.New()
                        .SetMessage(
                            string.Format(
                                "UseSelection is in a invalid state. Field {0}" +
                                " should never have been visited!",
                                selection.Field.Name))
                        .Build());
            }
        }

        protected override void LeaveList(IFieldSelection selection)
        {
            if (selection.Field.Member is PropertyInfo propertyInfo)
            {
                SelectionClosure closure = Closures.Pop();

                Expression body =
                    Expression.Property(
                        Closures.Peek().Instance.Peek(), propertyInfo);

                if (selection is FieldSelection fieldSelection)
                {
                    var context =
                        new SelectionVisitorContext(Context, _converter, fieldSelection);

                    for (var i = 0; i < _listHandler.Count; i++)
                    {
                        body = _listHandler[i].HandleLeave(context, selection, body);
                    }
                }

                Expression select =
                    closure.CreateSelection(body, propertyInfo.PropertyType);

                Closures.Peek().Projections[selection.Field.Name] =
                    Expression.Bind(selection.Field.Member, select);

                base.LeaveList(selection);
            }
            else
            {
                throw new QueryException(
                    ErrorBuilder.New()
                        .SetMessage(
                            string.Format(
                                "UseSelection is in a invalid state. Field {0}" +
                                " should never have been visited!",
                                selection.Field.Name))
                        .Build());
            }
        }

        protected override void EnterList(IFieldSelection selection)
        {
            if (selection.Field.Member is PropertyInfo)
            {
                (IOutputType type, SelectionSetNode selectionSet) =
                    UnwrapPaging(selection.Field.Type, selection.Selection.SelectionSet);

                Closures.Push(
                    new SelectionClosure(
                        type.ElementType().ToClrType(),
                        "e" + Closures.Count));

                VisitSelections(type, selectionSet);
            }
        }

        protected override void EnterObject(IFieldSelection selection)
        {
            if (selection.Field.Member is PropertyInfo property)
            {
                var nextClosure =
                    new SelectionClosure(
                        selection.Field.ClrType, "e" + Closures.Count);

                nextClosure.Instance.Push(
                    Expression.Property(
                        Closures.Peek().Instance.Peek(), property));

                Closures.Push(nextClosure);
                base.EnterObject(selection);
            }
        }
    }
}
