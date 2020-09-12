using System;
using System.Collections.Generic;
using HotChocolate.Language;

namespace HotChocolate.Execution.Utilities
{
    internal sealed partial class ResultHelper : IResultHelper
    {
        private readonly object _syncMap = new object();
        private readonly object _syncMapList = new object();
        private readonly object _syncList = new object();
        private readonly object _syncErrors = new object();
        private readonly List<IError> _errors = new List<IError>();
        private readonly HashSet<FieldNode> _fieldErrors = new HashSet<FieldNode>();
        private readonly List<NonNullViolation> _nonNullViolations = new List<NonNullViolation>();
        private readonly ResultPool _resultPool;
        private ResultMemoryOwner _resultOwner;
        private ResultMap? _data;

        public ResultHelper(ResultPool resultPool)
        {
            _resultPool = resultPool;
            _resultOwner = new ResultMemoryOwner(resultPool);
        }

        public IReadOnlyList<IError> Errors => _errors;

        public ResultMap RentResultMap(int capacity)
        {
            ResultMap? map;

            lock (_syncMap)
            {
                if (!_resultOwner.ResultMaps.TryPeek(out ResultObjectBuffer<ResultMap> buffer) ||
                    !buffer.TryPop(out map))
                {
                    buffer = _resultPool.GetResultMap();
                    map = buffer.Pop();
                    _resultOwner.ResultMaps.Push(buffer);
                }
            }

            map.EnsureCapacity(capacity);
            return map;
        }

        public ResultMapList RentResultMapList()
        {
            ResultMapList? mapList;

            lock (_syncMap)
            {
                if (!_resultOwner.ResultMapLists.TryPeek(
                    out ResultObjectBuffer<ResultMapList> buffer) ||
                    !buffer.TryPop(out mapList))
                {
                    buffer = _resultPool.GetResultMapList();
                    mapList = buffer.Pop();
                    _resultOwner.ResultMapLists.Push(buffer);
                }
            }

            return mapList;
        }

        public ResultList RentResultList()
        {
            ResultList? list;

            lock (_syncMap)
            {
                if (!_resultOwner.ResultLists.TryPeek(out ResultObjectBuffer<ResultList> buffer) ||
                    !buffer.TryPop(out list))
                {
                    buffer = _resultPool.GetResultList();
                    list = buffer.Pop();
                    _resultOwner.ResultLists.Push(buffer);
                }
            }

            return list;
        }

        public void SetData(ResultMap data)
        {
            _data = data;
        }

        public void AddError(IError error, FieldNode? selection = null)
        {
            lock (_syncErrors)
            {
                _errors.Add(error);
                if (selection is { })
                {
                    _fieldErrors.Add(selection);
                }
            }
        }

        public void AddErrors(IEnumerable<IError> errors, FieldNode? selection = null)
        {
            lock (_syncErrors)
            {
                _errors.AddRange(errors);

                if (selection is { })
                {
                    _fieldErrors.Add(selection);
                }
            }
        }

        public void AddNonNullViolation(FieldNode selection, Path path, IResultMap parent)
        {
            _nonNullViolations.Add(new NonNullViolation(selection, path, parent));
        }

        public IReadOnlyQueryResult BuildResult()
        {
            // TODO : add null errors
            while (_data != null && _nonNullViolations.TryPop(out NonNullViolation violation))
            {
                Path? path = violation.Path;
                IResultData? parent = violation.Parent;

                if (!_fieldErrors.Contains(violation.Selection))
                {
                    _errors.Add(ErrorBuilder.New()
                        .SetMessage("Cannot return null for non-nullable field.")
                        .SetCode("EXEC_NON_NULL_VIOLATION")
                        .SetPath(path)
                        .AddLocation(violation.Selection)
                        .Build());
                }

                while (parent != null)
                {
                    if (parent is ResultMap map &&
                        path is NamePathSegment nameSegment)
                    {
                        ResultValue value = map.GetValue(nameSegment.Name.Value, out int index);

                        if (value.IsNullable)
                        {
                            map.SetValue(index, value.Name, null, true);
                            break;
                        }
                        else
                        {
                            if (index != -1)
                            {
                                map.RemoveValue(index);
                            }
                            path = path.Parent;
                            parent = parent.Parent;

                            if (parent is null)
                            {
                                _data = null;
                                _resultOwner.Dispose();
                                break;
                            }
                        }
                    }
                    else if (parent is ResultMapList mapList &&
                        path is IndexerPathSegment mapListIndexSegment)
                    {

                        if (mapList.IsNullable)
                        {
                            mapList[mapListIndexSegment.Index] = null;
                            break;
                        }
                        else
                        {
                            path = path.Parent;
                            parent = parent.Parent;
                        }
                    }
                    else if (parent is ResultList list &&
                        path is IndexerPathSegment listIndexSegment)
                    {
                        if (list.IsNullable)
                        {
                            list[listIndexSegment.Index] = null;
                            break;
                        }
                        else
                        {
                            path = path.Parent;
                            parent = parent.Parent;
                        }
                    }
                    else
                    {
                        // TODO : ThrowHelper
                        throw new NotSupportedException();
                    }
                }
            }

            if (_data is null && _errors.Count == 0)
            {
                // TODO : ThrowHelper
                throw new InvalidOperationException();
            }

            return new QueryResult
            (
                data: _data,
                errors: _errors.Count == 0 ? null : new List<IError>(_errors),
                resultMemoryOwner: _data is null ? null : _resultOwner
            );
        }

        public void DropResult() => _resultOwner.Dispose();

        private readonly struct NonNullViolation
        {
            public NonNullViolation(FieldNode selection, Path path, IResultMap parent)
            {
                Selection = selection;
                Path = path;
                Parent = parent;
            }

            public FieldNode Selection { get; }
            public Path Path { get; }
            public IResultMap Parent { get; }
        }
    }
}