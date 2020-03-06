using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Types;
using HotChocolate.Utilities;

namespace HotChocolate.Execution
{
    internal sealed class CompleteValueContext
        : ICompleteValueContext
    {
        private ResolverContext _resolverContext;
        private FieldNode _selection;
        private SelectionSetNode _selectionSet;
        private Path _path;

        public void Clear()
        {
            _resolverContext = null;
            _selection = null;
            _selectionSet = null;
            _path = null;
            EnqueueNext = null;
            Value = null;
            HasErrors = false;
            IsViolatingNonNullType = false;
            SetElementNull = null;
        }

        public Action<ResolverContext> EnqueueNext { get; set; }

        public ResolverContext ResolverContext
        {
            get => _resolverContext;
            set
            {
                _resolverContext = value ?? throw new ArgumentNullException(nameof(value));
                _selection = _resolverContext.FieldSelection;
                _selectionSet = _selection.SelectionSet;
                _path = _resolverContext.Path;
                SetElementNull = null;
                IsViolatingNonNullType = false;
                HasErrors = false;
            }
        }

        public ITypeConversion Converter => _resolverContext.Converter;

        public Path Path
        {
            get => _path;
            set
            {
                _path = value ?? throw new ArgumentNullException(nameof(value));
            }
        }

        public object Value { get; set; }

        public bool HasErrors { get; private set; }

        public bool IsViolatingNonNullType { get; set; }

        public Action SetElementNull { get; set; }

        public IReadOnlyDictionary<string, object> LocalContextData =>
            ResolverContext.LocalContextData;

        public void AddError(Action<IErrorBuilder> error)
        {
            if (error is null)
            {
                throw new ArgumentNullException(nameof(error));
            }

            IErrorBuilder builder = ErrorBuilder.New();
            error(builder.SetPath(_path).AddLocation(_selection));
            AddError(builder.Build());
        }

        public void AddError(IError error)
        {
            if (error is null)
            {
                throw new ArgumentNullException(nameof(error));
            }

            HasErrors = true;
            _resolverContext.ReportError(error);
        }

        public FieldData EnqueueForProcessing(ObjectType objectType, object resolverResult)
        {
            IReadOnlyList<FieldSelection> fields =
                ResolverContext.CollectFields(
                    objectType, _selectionSet);

            IImmutableStack<object> source =
                _resolverContext.Source.Push(resolverResult);

            Action listNonNullViolationPropagation =
                CreateListNonNullViolationPropagation(
                        ResolverContext, SetElementNull);

            var serializedResult = new FieldData(fields.Count);

            for(int i = 0; i < fields.Count; i++)
            {
                FieldSelection selection = fields[i];

                EnqueueNext(_resolverContext.Branch(
                    selection,
                    source,
                    resolverResult,
                    serializedResult,
                    Path.Append(selection.ResponseName),
                    listNonNullViolationPropagation));
            }

            return serializedResult;
        }

        private static Action CreateListNonNullViolationPropagation(
            ResolverContext resolverContext,
            Action setElementNull)
        {
            if (setElementNull is null)
            {
                return null;
            }

            bool isNonNullType =
                resolverContext.Field.Type.ElementType().IsNonNullType();
            Action propagateNonNullViolation =
                resolverContext.PropagateNonNullViolation;

            return () =>
            {
                setElementNull.Invoke();

                if (isNonNullType)
                {
                    propagateNonNullViolation();
                }
            };
        }

        public ObjectType ResolveObjectType(IType type, object resolverResult)
        {
            if (type is ObjectType objectType)
            {
                return objectType;
            }
            else if (type is InterfaceType interfaceType)
            {
                return interfaceType.ResolveType(
                    ResolverContext,
                    resolverResult);
            }
            else if (type is UnionType unionType)
            {
                return unionType.ResolveType(
                    ResolverContext,
                    resolverResult);
            }

            throw new NotSupportedException(
                CoreResources.ResolveObjectType_TypeNotSupported);
        }

        public ObjectType ResolveObjectType(NameString typeName)
        {
            if (ResolverContext.Schema.TryGetType(typeName, out ObjectType type))
            {
                return type;
            }
            return null;
        }
    }
}
