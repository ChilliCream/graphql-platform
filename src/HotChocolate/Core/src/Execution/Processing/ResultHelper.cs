using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using HotChocolate.Execution.Properties;
using HotChocolate.Language;

namespace HotChocolate.Execution.Processing
{
    internal sealed partial class ResultHelper : IResultHelper
    {
        private readonly object _syncMap = new();
        private readonly object _syncMapList = new();
        private readonly object _syncList = new();
        private readonly object _syncErrors = new();
        private readonly object _syncExtensions = new();
        private readonly List<IError> _errors = new();
        private readonly HashSet<FieldNode> _fieldErrors = new();
        private readonly List<NonNullViolation> _nonNullViolations = new();
        private readonly ResultPool _resultPool;
        private readonly Dictionary<string, object?> _extensions = new();
        private readonly Dictionary<string, object?> _contextData = new();
        private ResultMemoryOwner _resultOwner;
        private ResultMap? _data;
        private Path? _path;
        private string? _label;
        private bool? _hasNext;

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

            lock (_syncMapList)
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

            lock (_syncList)
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

        public void SetExtension(string key, object? value)
        {
            lock (_syncExtensions)
            {
                _extensions[key] = value;
            }
        }

        public void SetContextData(string key, object? value)
        {
            lock (_syncExtensions)
            {
                _contextData[key] = value;
            }
        }

        public void SetPath(Path? path)
        {
            _path = path;
        }

        public void SetLabel(string? label)
        {
            _label = label;
        }

        public void SetHasNext(bool value)
        {
            _hasNext = value;
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

        public IQueryResult BuildResult()
        {
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
                    if (parent is ResultMap map && path is NamePathSegment nameSegment)
                    {
                        ResultValue value = map.GetValue(nameSegment.Name.Value, out var index);

                        if (value.IsNullable)
                        {
                            map.SetValue(index, value.Name, value: null, isNullable: true);
                            break;
                        }

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
                    else if (parent is ResultMapList mapList &&
                        path is IndexerPathSegment mapListIndexSegment)
                    {
                        if (mapList.IsNullable)
                        {
                            mapList[mapListIndexSegment.Index] = null;
                            break;
                        }

                        path = path.Parent;
                        parent = parent.Parent;
                    }
                    else if (parent is ResultList list &&
                        path is IndexerPathSegment listIndexSegment)
                    {
                        if (list.IsNullable)
                        {
                            list[listIndexSegment.Index] = null;
                            break;
                        }

                        path = path.Parent;
                        parent = parent.Parent;
                    }
                    else
                    {
                        break;
                    }
                }
            }

            if (_data is null && _errors.Count == 0)
            {
                throw new InvalidOperationException(
                    Resources.ResultHelper_BuildResult_InvalidResult);
            }

            return new QueryResult
            (
                _data,
                _errors.Count == 0 ? null : new List<IError>(_errors),
                CreateExtensionData(_extensions),
                CreateExtensionData(_contextData),
                _label,
                _path,
                _hasNext,
                resultMemoryOwner: _data is null ? null : _resultOwner
            );
        }

        private IReadOnlyDictionary<string, object?>? CreateExtensionData(
            Dictionary<string, object?> data)
        {
            if (data.Count == 0)
            {
                return null;
            }

            if (data.Count == 1)
            {
                KeyValuePair<string, object?> value = data.Single();
                return new SingleValueExtensionData(value.Key, value.Value);
            }

            return ImmutableDictionary.CreateRange(data);
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
