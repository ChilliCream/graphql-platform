using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Utilities;

namespace HotChocolate.Execution
{
    internal sealed class FieldValueCompletionContext
        : IFieldValueCompletionContext
    {
        private readonly Action<object> _integrateResult;
        private readonly Action<ResolverTask> _enqueueResolverTask;
        private readonly ResolverTask _resolverTask;

        public FieldValueCompletionContext(
            IExecutionContext executionContext,
            IResolverContext resolverContext,
            ResolverTask resolverTask,
            Action<ResolverTask> enqueueTask)
        {
            ExecutionContext = executionContext
                ?? throw new ArgumentNullException(nameof(executionContext));
            ResolverContext = resolverContext
                ?? throw new ArgumentNullException(nameof(resolverContext));
            _resolverTask = resolverTask
                ?? throw new ArgumentNullException(nameof(resolverTask));
            _enqueueResolverTask = enqueueTask
                ?? throw new ArgumentNullException(nameof(enqueueTask));

            _integrateResult = resolverTask.IntegrateResult;

            Source = resolverContext.Source;
            Selection = resolverTask.FieldSelection;
            SelectionSet = resolverTask.FieldSelection.Selection.SelectionSet;
            Type = resolverContext.Field.Type;
            Path = resolverContext.Path;
            Value = resolverTask.ResolverResult;
            Converter = executionContext.Services.GetTypeConversion();
            IsNullable = true;
        }

        private FieldValueCompletionContext(
            FieldValueCompletionContext completionContext,
            IType type, bool isNullable)
        {
            _integrateResult = completionContext._integrateResult;
            _enqueueResolverTask = completionContext._enqueueResolverTask;
            _resolverTask = completionContext._resolverTask;

            ExecutionContext = completionContext.ExecutionContext;
            ResolverContext = completionContext.ResolverContext;
            Source = completionContext.Source;
            Selection = completionContext.Selection;
            SelectionSet = completionContext.SelectionSet;
            Path = completionContext.Path;
            Value = completionContext.Value;
            Converter = completionContext.Converter;

            Type = type;
            IsNullable = isNullable;
        }

        private FieldValueCompletionContext(
            FieldValueCompletionContext completionContext,
            Element element,
            Action<object> addElementToList)
        {
            _integrateResult = addElementToList;
            _enqueueResolverTask = completionContext._enqueueResolverTask;
            _resolverTask = completionContext._resolverTask;

            ExecutionContext = completionContext.ExecutionContext;
            ResolverContext = completionContext.ResolverContext;
            Source = completionContext.Source;
            Selection = completionContext.Selection;
            SelectionSet = completionContext.SelectionSet;
            IsNullable = completionContext.IsNullable;
            Converter = completionContext.Converter;

            Path = element.Path;
            Type = element.Type;
            Value = element.Value;
        }

        public IExecutionContext ExecutionContext { get; }

        public IResolverContext ResolverContext { get; }

        public IImmutableStack<object> Source { get; }

        public FieldSelection Selection { get; }

        public SelectionSetNode SelectionSet { get; }

        public IType Type { get; }

        public Path Path { get; }

        public object Value { get; }

        public bool IsNullable { get; }

        public ITypeConversion Converter { get; }

        public void ReportError(IEnumerable<IError> errors)
        {
            if (errors == null)
            {
                throw new ArgumentNullException(nameof(errors));
            }

            foreach (IError error in errors)
            {
                ReportError(error);
            }

            _integrateResult(null);
        }

        public void ReportError(IError error)
        {
            if (error == null)
            {
                throw new ArgumentNullException(nameof(error));
            }

            _resolverTask.ReportError(error);
            _integrateResult(null);
        }

        public void ReportError(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                throw new ArgumentNullException(nameof(message));
            }

            ReportError(QueryError.CreateFieldError(
                message, Path, Selection.Selection));
            _integrateResult(null);
        }

        private void ReportNonNullError()
        {
            ReportError(QueryError.CreateFieldError(
                "Cannot return null for non-nullable field.",
                Path,
                Selection.Selection));
        }

        public void IntegrateResult(object value)
        {
            _integrateResult(value);
            if (!IsNullable && value == null)
            {
                ReportNonNullError();
            }
        }

        public void EnqueueForProcessing(
            ObjectType objectType,
            OrderedDictionary objectResult)
        {
            IReadOnlyCollection<FieldSelection> fields =
                ExecutionContext.FieldHelper
                    .CollectFields(objectType, SelectionSet);

            foreach (FieldSelection field in fields)
            {
                _enqueueResolverTask(new ResolverTask(
                    ExecutionContext, objectType, field,
                    Path.Append(field.ResponseName),
                    Source.Push(Value), objectResult));
            }
        }

        public IFieldValueCompletionContext AsNonNullValueContext()
        {
            if (Type.IsNonNullType())
            {
                return new FieldValueCompletionContext(
                    this, Type.InnerType(), false);
            }

            throw new InvalidOperationException(
                "The current type is not a non-null type.");
        }

        public IFieldValueCompletionContext AsElementValueContext(
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
                this,
                new Element(elementPath, elementType, element),
                addElementToList);
        }

        private readonly struct Element
        {
            public Element(Path path, IType type, object value)
            {
                Path = path ?? throw new ArgumentNullException(nameof(path));
                Type = type ?? throw new ArgumentNullException(nameof(type));
                Value = value;
            }

            public Path Path { get; }
            public IType Type { get; }
            public object Value { get; }
        }
    }
}
