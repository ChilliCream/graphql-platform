using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Utilities;

namespace HotChocolate.Execution
{
    internal sealed class CompleteValueContext
        : ICompleteValueContext
    {
        private readonly IFieldHelper _fieldHelper;
        private readonly Action<ResolverTask> _enqueueTask;
        private ResolverTask _resolverTask;
        private FieldNode _selection;
        private SelectionSetNode _selectionSet;
        private Path _path;

        public CompleteValueContext(
            IFieldHelper fieldHelper,
            Action<ResolverTask> enqueueTask)
        {
            _fieldHelper = fieldHelper
                ?? throw new ArgumentNullException(nameof(fieldHelper));
            _enqueueTask = enqueueTask
                ?? throw new ArgumentNullException(nameof(enqueueTask));
        }

        public ResolverTask ResolverTask
        {
            get => _resolverTask;
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                _resolverTask = value;
                _selection = _resolverTask.FieldSelection.Selection;
                _selectionSet = _selection.SelectionSet;
                _path = _resolverTask.Path;
                SetElementNull = null;
                IsViolatingNonNullType = false;
                HasErrors = false;
            }
        }

        public ITypeConversion Converter { get; }

        public Path Path
        {
            get => _path;
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                _path = value;
            }
        }

        public object Value { get; set; }

        public bool HasErrors { get; private set; }

        public bool IsViolatingNonNullType { get; set; }

        public Action SetElementNull { get; set; }

        public void AddError(Action<IErrorBuilder> error)
        {
            if (error == null)
            {
                throw new ArgumentNullException(nameof(error));
            }

            IErrorBuilder builder = ErrorBuilder.New();
            error(builder.SetPath(_path).AddLocation(_selection));
            AddError(builder.Build());
        }

        public void AddError(IError error)
        {
            if (error == null)
            {
                throw new ArgumentNullException(nameof(error));
            }

            HasErrors = true;
            _resolverTask.ReportError(error);
        }

        public void EnqueueForProcessing(
            ObjectType objectType,
            OrderedDictionary objectResult,
            object resolverResult)
        {
            IReadOnlyCollection<FieldSelection> fields =
                _fieldHelper.CollectFields(
                    objectType, _selectionSet);

            IImmutableStack<object> source =
                _resolverTask.Source.Push(resolverResult);

            foreach (FieldSelection field in fields)
            {
                _enqueueTask(_resolverTask.Branch(
                    objectType, field,
                    Path.Append(field.ResponseName),
                    source, objectResult,
                    CreateListNonNullViolationPropagation(
                        _resolverTask, SetElementNull)));
            }
        }

        private static Action CreateListNonNullViolationPropagation(
            ResolverTask resolverTask,
            Action setElementNull)
        {
            if (setElementNull == null)
            {
                return null;
            }

            return () =>
            {
                setElementNull.Invoke();

                if (resolverTask.FieldType.ElementType().IsNonNullType())
                {
                    resolverTask.PropagateNonNullViolation();
                }
            };
        }

        public ObjectType ResolveObjectType(IType type)
        {
            if (type is ObjectType objectType)
            {
                return objectType;
            }
            else if (type is InterfaceType interfaceType)
            {
                return interfaceType.ResolveType(
                    _resolverTask.ResolverContext,
                    _resolverTask.ResolverResult);
            }
            else if (type is UnionType unionType)
            {
                return unionType.ResolveType(
                    _resolverTask.ResolverContext,
                    _resolverTask.ResolverResult);
            }

            // TODO : resources
            throw new NotSupportedException(
                "The specified type is not supported.");
        }
    }
}
