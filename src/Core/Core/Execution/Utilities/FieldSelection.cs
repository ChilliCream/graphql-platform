using System.Linq;
using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using System;
using System.Collections.Immutable;

namespace HotChocolate.Execution
{
    public sealed class FieldSelection
        : IFieldSelection
    {
        private readonly IReadOnlyDictionary<NameString, ArgumentValue> _args;
        private readonly IReadOnlyDictionary<NameString, VariableValue> _varArgs;
        private readonly IReadOnlyList<FieldVisibility> _visibility;
        private readonly Path _path;
        private readonly bool _hasArgumentErrors;

        internal FieldSelection(FieldInfo fieldInfo)
        {
            _args = fieldInfo.Arguments;
            _varArgs = fieldInfo.VarArguments;
            _visibility = fieldInfo.Visibilities;
            _path = fieldInfo.Path;
            _hasArgumentErrors =
                fieldInfo.Arguments != null
                && fieldInfo.Arguments.Any(t => t.Value.Error != null);

            ResponseName = fieldInfo.ResponseName;
            Field = fieldInfo.Field;
            Selection = MergeField(fieldInfo);

            var nodes = new List<FieldNode>();
            nodes.Add(fieldInfo.Selection);
            if (fieldInfo.Nodes != null)
            {
                nodes.AddRange(fieldInfo.Nodes);
            }

            Middleware = fieldInfo.Middleware;
        }

        public string ResponseName { get; }

        public ObjectField Field { get; }

        public FieldNode Selection { get; }

        public IReadOnlyList<FieldNode> Nodes { get; }

        public FieldDelegate Middleware { get; }

        public IReadOnlyDictionary<NameString, ArgumentValue> CoerceArguments(
            IVariableCollection variables)
        {
            if (_hasArgumentErrors)
            {
                Path path = CreatePath();
                throw new QueryException(
                    _args.Values.Select(t => t.Error.WithPath(path)));
            }

            if (_varArgs == null)
            {
                return _args;
            }

            var args = _args == null
                ? new Dictionary<NameString, ArgumentValue>()
                : _args.ToDictionary(t => t.Key, t => t.Value);

            foreach (KeyValuePair<NameString, VariableValue> var in _varArgs)
            {
                if (!variables.TryGetVariable(
                    var.Value.VariableName,
                    out object value))
                {
                    value = var.Value.DefaultValue;
                }

                IError error = InputTypeNonNullCheck.CheckForNullValueViolation(
                    var.Key,
                    var.Value.Type,
                    value,
                    message => ErrorBuilder.New()
                        .SetMessage(message)
                        .SetPath(CreatePath())
                        .AddLocation(Selection)
                        .SetExtension("argument", var.Key)
                        .Build());

                if (error is null)
                {
                    args[var.Key] = new ArgumentValue(var.Value.Type, value);
                }
                else
                {
                    throw new QueryException(error);
                }
            }

            return args;
        }

        public bool IsVisible(IVariableCollection variables)
        {
            if (_visibility == null || _visibility.Count == 0)
            {
                return true;
            }

            for (int i = 0; i < _visibility.Count; i++)
            {
                if (!_visibility[i].IsVisible(variables))
                {
                    return false;
                }
            }

            return true;
        }

        private Path CreatePath() =>
            _path == null
                ? Path.New(ResponseName)
                : _path.Append(ResponseName);

        private static FieldNode MergeField(FieldInfo fieldInfo)
        {
            if (fieldInfo.Nodes == null)
            {
                return fieldInfo.Selection;
            }

            return new FieldNode
            (
                fieldInfo.Selection.Location,
                fieldInfo.Selection.Name,
                fieldInfo.Selection.Alias,
                MergeDirectives(fieldInfo),
                fieldInfo.Selection.Arguments,
                MergeSelections(fieldInfo)
            );
        }

        private static SelectionSetNode MergeSelections(FieldInfo fieldInfo)
        {
            if (fieldInfo.Selection.SelectionSet == null)
            {
                return null;
            }

            var selections = new List<ISelectionNode>();
            selections.AddRange(fieldInfo.Selection.SelectionSet.Selections);

            foreach (FieldNode selection in fieldInfo.Nodes)
            {
                if (selection.SelectionSet != null)
                {
                    selections.AddRange(selection.SelectionSet.Selections);
                }
            }

            return new SelectionSetNode
            (
                fieldInfo.Selection.SelectionSet.Location,
                selections
            );
        }

        private static IReadOnlyList<DirectiveNode> MergeDirectives(
            FieldInfo fieldInfo)
        {
            var directives = new List<DirectiveNode>();
            directives.AddRange(fieldInfo.Selection.Directives);

            foreach (FieldNode selection in fieldInfo.Nodes)
            {
                directives.AddRange(selection.Directives);
            }

            return directives;
        }
    }
}
