using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Execution;
using HotChocolate.Resolvers;

namespace HotChocolate.Types.Selection
{
    public class SelectionVisitor
        : SelectionVisitorBase
    {
        public SelectionVisitor(IResolverContext context)
            : base(context)
        {
        }

        protected Stack<SelectionClosure> Closures { get; } =
            new Stack<SelectionClosure>();

        public void Accept(ObjectField field)
        {
            Closures.Push(new SelectionClosure(field.Type.ElementType().ToClrType(), "e"));
            VisitSelections(field, Context.FieldSelection.SelectionSet);
        }

        public Expression<Func<T, T>> Project<T>()
        {
            return (Expression<Func<T, T>>)Closures.Peek().CreateMemberInitLambda();
        }

        protected override void LeaveLeaf(IFieldSelection selection)
        {
            SelectionClosure closure = Closures.Peek();
            if (selection.Field.Member is PropertyInfo member)
            {
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

                MemberExpression property =
                    Expression.Property(
                        Closures.Peek().Instance.Peek(), propertyInfo);

                Expression select =
                    closure.CreateSelection(
                        property, propertyInfo.PropertyType);

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
                Closures.Push(
                new SelectionClosure(
                    selection.Field.Type.ElementType().ToClrType(),
                    "e" + Closures.Count));
                base.EnterList(selection);
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
