using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Execution
{
    internal readonly struct FieldValueCompletionContext
    {
        private readonly Action<object> _setResult;

        public FieldValueCompletionContext(
            ExecutionContext executionContext,
            IResolverContext resolverContext,
            FieldSelection selection,
            Action<object> setResult,
            object value)
        {
            if (executionContext == null)
            {
                throw new ArgumentNullException(nameof(executionContext));
            }

            if (resolverContext == null)
            {
                throw new ArgumentNullException(nameof(resolverContext));
            }

            if (selection == null)
            {
                throw new ArgumentNullException(nameof(selection));
            }

            if (setResult == null)
            {
                throw new ArgumentNullException(nameof(setResult));
            }

            _setResult = setResult;

            ExecutionContext = executionContext;
            ResolverContext = resolverContext;
            Source = resolverContext.Source;
            Selection = selection;
            SelectionSet = selection.Node.SelectionSet;
            Type = resolverContext.Field.Type;
            Path = resolverContext.Path;
            Value = value;
            IsNullable = true;
        }

        private FieldValueCompletionContext(
            FieldValueCompletionContext context,
            IType type, bool isNullable)
        {
            _setResult = context._setResult;
            ExecutionContext = context.ExecutionContext;
            ResolverContext = context.ResolverContext;
            Source = context.Source;
            Selection = context.Selection;
            SelectionSet = context.SelectionSet;
            Path = context.Path;
            Value = context.Value;

            Type = type;
            IsNullable = isNullable;
        }

        private FieldValueCompletionContext(
            FieldValueCompletionContext context,
            Path elementPath, IType elementType,
            object element, Action<object> addElementToList)
        {
            ExecutionContext = context.ExecutionContext;
            ResolverContext = context.ResolverContext;
            Source = context.Source;
            Selection = context.Selection;
            SelectionSet = context.SelectionSet;
            IsNullable = context.IsNullable;

            Path = elementPath;
            Type = elementType;
            Value = element;
            _setResult = addElementToList;
        }

        public ExecutionContext ExecutionContext { get; }
        public IResolverContext ResolverContext { get; }
        public ImmutableStack<object> Source { get; }
        public FieldSelection Selection { get; }
        public SelectionSetNode SelectionSet { get; }
        public IType Type { get; }
        public Path Path { get; }
        public object Value { get; }
        public bool IsNullable { get; }

        public void AddErrors(IEnumerable<IQueryError> errors)
        {
            if (errors == null)
            {
                throw new ArgumentNullException(nameof(errors));
            }

            ExecutionContext.Errors.AddRange(errors);
            _setResult(null);
        }

        public void AddError(IQueryError error)
        {
            if (error == null)
            {
                throw new ArgumentNullException(nameof(error));
            }

            ExecutionContext.Errors.Add(error);
            _setResult(null);
        }

        public void AddError(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                throw new ArgumentNullException(nameof(message));
            }

            ExecutionContext.Errors.Add(new FieldError(message, Selection.Node));
            _setResult(null);
        }

        private void AddNonNullError()
        {
            AddError(new FieldError(
                "Cannot return null for non-nullable field.",
                Selection.Node));
        }

        public void SetResult(object value)
        {
            _setResult(value);
            if (!IsNullable && value == null)
            {
                AddNonNullError();
            }
        }

        public FieldValueCompletionContext AsNonNullValueContext()
        {
            if (Type.IsNonNullType())
            {
                return new FieldValueCompletionContext(this, Type.InnerType(), true);
            }

            throw new InvalidOperationException(
                "The current type is not a non-null type.");
        }

        public FieldValueCompletionContext AsElementValueContext(
            Path elementPath, IType elementType,
            object element, Action<object> addElementToList)
        {
            if (elementPath == null)
            {
                throw new ArgumentNullException(nameof(elementPath));
            }

            if (elementType == null)
            {
                throw new ArgumentNullException(nameof(elementType));
            }

            if (addElementToList == null)
            {
                throw new ArgumentNullException(nameof(addElementToList));
            }

            return new FieldValueCompletionContext(
                this, elementPath, elementType, element, addElementToList);
        }
    }
}
