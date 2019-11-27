using System.Collections.Immutable;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Utilities;
using HotChocolate.Properties;

namespace HotChocolate.Execution
{
    public sealed class FieldSelection
        : IFieldSelection
    {
        private const string _argumentProperty = "argument";

        private static IReadOnlyDictionary<NameString, ArgumentValue> _empty =
            ImmutableDictionary<NameString, ArgumentValue>.Empty;
        private readonly IReadOnlyDictionary<NameString, ArgumentValue> _args;
        private readonly IReadOnlyDictionary<NameString, ArgumentVariableValue> _vars;
        private readonly IReadOnlyList<FieldVisibility> _visibility;
        private readonly Path _path;
        private readonly bool _hasArgumentErrors;

        internal FieldSelection(FieldInfo fieldInfo)
        {
            _args = fieldInfo.Arguments ?? _empty;
            _vars = fieldInfo.VarArguments;
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
            IVariableValueCollection variables,
            ITypeConversion converter)
        {
            if (_hasArgumentErrors)
            {
                throw new QueryException(_args.Values.Select(t => t.Error));
            }

            if (_vars == null)
            {
                return _args;
            }

            var args = _args.ToDictionary(t => t.Key, t => t.Value);

            foreach (KeyValuePair<NameString, ArgumentVariableValue> var in _vars)
            {
                IError error = null;

                if (variables.TryGetVariable(
                    var.Value.VariableName,
                    out object value))
                {
                    value = var.Value.CoerceValue(value);
                }
                else
                {
                    value = var.Value.DefaultValue;

                    if (var.Value.Type.NamedType().IsLeafType()
                        && value is IValueNode literal)
                    {
                        value = var.Value.Type.ParseLiteral(literal);
                    }

                    if (var.Value.Type.IsNonNullType()
                        && (value is null || value is NullValueNode))
                    {
                        error = ErrorBuilder.New()
                            .SetMessage(string.Format(
                                CultureInfo.InvariantCulture,
                                TypeResources.ArgumentValueBuilder_NonNull,
                                var.Key,
                                TypeVisualizer.Visualize(var.Value.Type)))
                            .AddLocation(Selection)
                            .SetExtension(_argumentProperty, Path.New(var.Key))
                            .SetPath(_path)
                            .Build();
                    }
                }

                if (error is null)
                {
                    ValueKind kind = ValueKind.Unknown;

                    if (value is IValueNode literal)
                    {
                        kind = literal.GetValueKind();
                        args[var.Key] = new ArgumentValue(var.Value.Argument, kind, literal);
                    }
                    else
                    {
                        Scalars.TryGetKind(value, out kind);
                        args[var.Key] = new ArgumentValue(var.Value.Argument, kind, value);
                    }
                }
                else
                {
                    throw new QueryException(error);
                }
            }

            return args;
        }

        public bool IsVisible(IVariableValueCollection variables)
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
