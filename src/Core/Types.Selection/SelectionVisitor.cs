using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types.Filters;
using HotChocolate.Types.Sorting;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Selection
{
    public class SelectionVisitor
        : SelectionVisitorBase
    {
        private readonly ITypeConversion _converter;

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
            SelectionSetNode selectionSet = Context.FieldSelection.SelectionSet;
            (field, selectionSet) = UnwrapPaging(field, selectionSet);
            Closures.Push(new SelectionClosure(field.Type.ElementType().ToClrType(), "e"));
            VisitSelections(field, selectionSet);
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

                Expression body =
                    Expression.Property(
                        Closures.Peek().Instance.Peek(), propertyInfo);

                if (selection is FieldSelection fieldSelection)
                {
                    IReadOnlyDictionary<NameString, ArgumentValue> arguments
                        = fieldSelection.CoerceArguments(
                            Context.Variables, _converter);

                    body = ProjectSorting(selection, body, arguments);
                    body = ProjectFilters(selection, body, arguments);
                }

                Expression select =
                    closure.CreateSelection(
                        body, propertyInfo.PropertyType);

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
                selection = UnwrapPaging(selection);
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

        private Expression ProjectFilters(
            IFieldSelection selection,
            Expression expression,
            IReadOnlyDictionary<NameString, ArgumentValue> arguments)
        {
            if (TryGetValueNode(arguments, "where", out IValueNode filter) &&
                selection.Field.Arguments["where"].Type is InputObjectType iot &&
                iot is IFilterInputType fit)
            {
                var visitor = new QueryableFilterVisitor(iot, fit.EntityType, _converter);

                filter.Accept(visitor);

                return Expression.Call(
                    typeof(Enumerable),
                    "Where",
                    new[] { fit.EntityType },
                    expression,
                    visitor.CreateFilter());
            }
            return expression;
        }

        private Expression ProjectSorting(
            IFieldSelection selection,
            Expression expression,
            IReadOnlyDictionary<NameString, ArgumentValue> arguments)
        {
            const string argName = SortObjectFieldDescriptorExtensions.OrderByArgumentName;
            if (TryGetValueNode(arguments, argName, out IValueNode sortArgument) &&
                selection.Field.Arguments[argName].Type is InputObjectType iot &&
                iot is ISortInputType fit)
            {
                var visitor = new QueryableSortVisitor(iot, fit.EntityType);

                sortArgument.Accept(visitor);
                return visitor.Compile(expression);
            }
            return expression;
        }

        private bool TryGetValueNode(
            IReadOnlyDictionary<NameString, ArgumentValue> arguments,
            string key,
            out IValueNode arg)
        {
            if (arguments.TryGetValue(key, out ArgumentValue argumentValue) &&
                argumentValue.Literal != null &&
                !(argumentValue.Literal is NullValueNode))
            {
                EnsureNoError(argumentValue);

                IValueNode literal = argumentValue.Literal;

                arg = VariableToValueRewriter.Rewrite(
                    literal,
                    argumentValue.Type,
                     Context.Variables, _converter);
                return true;
            }
            arg = null;
            return false;
        }

        private void EnsureNoError(ArgumentValue argumentValue)
        {
            if (argumentValue.Error != null)
            {
                throw new QueryException(argumentValue.Error);
            }
        }
    }
}
