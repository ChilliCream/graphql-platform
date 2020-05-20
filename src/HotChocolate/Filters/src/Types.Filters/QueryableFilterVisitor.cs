using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Types.Filters.Conventions;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Filters
{
    public class FilterScope<T>
    {
        public FilterScope()
        {
            Level = new Stack<Queue<T>>();
            Instance = new Stack<T>();
            Level.Push(new Queue<T>());
        }

        public Stack<Queue<T>> Level { get; }

        public Stack<T> Instance { get; }
    }

    public abstract class FilterVisitorContext<T>
        : IFilterVisitorContext<T>
    {
        protected FilterVisitorContext(
            IFilterInputType initialType,
            FilterVisitorDefinition<T> defintion,
            ITypeConversion typeConverter)
        {
            if (initialType is null)
            {
                throw new ArgumentNullException(nameof(initialType));
            }
            Definition = defintion ??
                throw new ArgumentNullException(nameof(defintion));
            TypeConverter = typeConverter ??
                throw new ArgumentNullException(nameof(typeConverter));

            Types.Push(initialType);
            Scopes = new Stack<FilterScope<T>>();
            Scopes.Push(CreateScope());
        }

        protected FilterVisitorDefinition<T> Definition { get; }

        public ITypeConversion TypeConverter { get; }

        public Stack<FilterScope<T>> Scopes { get; }

        public Stack<IType> Types { get; } = new Stack<IType>();

        public Stack<IInputField> Operations { get; } = new Stack<IInputField>();

        public IList<IError> Errors { get; } = new List<IError>();

        public bool TryGetEnterHandler(
            FilterKind kind,
            [NotNullWhen(true)] out FilterFieldEnter<T>? enter)
        {
            if (Definition.FieldHandler.TryGetValue(
                kind, out (FilterFieldEnter<T>? enter, FilterFieldLeave<T>? leave) val) &&
                val.enter is FilterFieldEnter<T>)
            {
                enter = val.enter;
                return true;
            }
            enter = null;
            return false;
        }

        public bool TryGetLeaveHandler(
            FilterKind kind,
            [NotNullWhen(true)] out FilterFieldLeave<T>? leave)
        {
            if (Definition.FieldHandler.TryGetValue(
                kind, out (FilterFieldEnter<T>? enter, FilterFieldLeave<T>? leave) val) &&
                val.leave is FilterFieldLeave<T>)
            {
                leave = val.leave;
                return true;
            }
            leave = null;
            return false;
        }

        public bool TryGetOperation(
            FilterKind kind,
            FilterOperationKind operationKind,
            [NotNullWhen(true)] out FilterOperationHandler<T>? handler)
        {
            if (Definition.OperationHandler.TryGetValue(
                (kind, operationKind), out FilterOperationHandler<T>? operationHandler))
            {
                handler = operationHandler;
                return true;
            }
            handler = null;
            return false;
        }

        public bool TryCombineOperations(
            Queue<T> operations,
            FilterCombinator combinator,
            [NotNullWhen(true)] out T combined)
        {
            if (operations.Count != 0 &&
                Definition.OperationCombinator.TryGetValue(
                    combinator, out FilterOperationCombinator<T>? combine))
            {
                combined = operations.Dequeue();

                while (operations.Count != 0)
                {
                    combined = combine(combined, operations.Dequeue());
                }

                return true;
            }

            combined = default!;
            return false;
        }

        public virtual FilterScope<T> CreateScope()
        {
            return new FilterScope<T>();
        }
    }

    public enum FilterCombinator
    {
        AND, OR
    }

    public class FilterVisitor<T>
        : FilterVisitorBase<FilterVisitorContext<T>>
    {
        protected FilterVisitor()
        {
        }

        protected override ISyntaxVisitorAction Enter(
            ObjectValueNode node,
            FilterVisitorContext<T> context)
        {
            context.PushLevel(new Queue<T>());
            return Continue;
        }

        protected override ISyntaxVisitorAction Leave(
            ObjectValueNode node,
            FilterVisitorContext<T> context)
        {
            Queue<T> operations = context.PopLevel();

            if (context.TryCombineOperations(
                operations,
                FilterCombinator.AND,
                out T combined))
            {
                context.GetLevel().Enqueue(combined);
            }

            return Continue;
        }

        protected override ISyntaxVisitorAction Enter(
            ObjectFieldNode node,
            FilterVisitorContext<T> context)
        {
            base.Enter(node, context);

            if (context.Operations.Peek() is FilterOperationField field)
            {
                if (context.TryGetEnterHandler(
                    field.Operation.FilterKind, out FilterFieldEnter<T>? enter) &&
                        enter(
                            field,
                            node,
                            context,
                            out ISyntaxVisitorAction? action))
                {
                    return action;
                }

                if (context.TryGetOperation(
                    field.Operation.FilterKind,
                    field.Operation.Kind,
                    out FilterOperationHandler<T>? handler) &&
                    handler(field.Operation, field.Type,
                        node.Value, context, out T expression))
                {
                    context.GetLevel().Enqueue(expression);
                }
                return SkipAndLeave;
            }
            return Continue;
        }

        protected override ISyntaxVisitorAction Leave(
            ObjectFieldNode node,
            FilterVisitorContext<T> context)
        {
            if (context.Operations.Peek() is FilterOperationField field)
            {
                if (context.TryGetLeaveHandler(
                    field.Operation.FilterKind, out FilterFieldLeave<T>? leave))
                {
                    leave(field, node, context);
                }
            }
            return base.Leave(node, context);
        }

        protected override ISyntaxVisitorAction Enter(
            ListValueNode node,
            FilterVisitorContext<T> context)
        {
            context.PushLevel(new Queue<T>());
            return Continue;
        }

        protected override ISyntaxVisitorAction Leave(
            ListValueNode node,
            FilterVisitorContext<T> context)
        {
            FilterCombinator combinator =
                context.Operations.Peek() is OrField
                    ? FilterCombinator.OR
                    : FilterCombinator.AND;

            Queue<T> operations = context.PopLevel();

            if (context.TryCombineOperations(
                operations,
                combinator,
                out T combined))
            {
                context.GetLevel().Enqueue(combined);
            }

            return Continue;
        }

        // TODO: DI
        public static FilterVisitor<T> Default = new FilterVisitor<T>();
    }
}
